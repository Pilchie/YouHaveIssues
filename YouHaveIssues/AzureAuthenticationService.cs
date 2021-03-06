﻿using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace YouHaveIssues
{
    public class AzureADOptions
    {
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? TenantId { get; set; }
    }

    public class AzureAuthenticationService
    {
        private readonly SemaphoreSlim _acquireTokenLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private readonly IConfidentialClientApplication _app;
        private readonly ILogger<AzureAuthenticationService> _logger;
        private readonly string[] _scopes;

        private AuthenticationResult? _token;

        public AzureAuthenticationService(IOptionsMonitor<AzureADOptions> azureAdOptions, IOptions<KustoOptions> kustoOptions, ILogger<AzureAuthenticationService> logger)
        {
            _logger = logger;

            var adOptions = azureAdOptions.CurrentValue;

            var builder = ConfidentialClientApplicationBuilder.Create(adOptions.ClientId)
                .WithTenantId(adOptions.TenantId);

            if (!string.IsNullOrEmpty(adOptions.ClientSecret))
            {
                _logger.LogInformation("Authenticating using Client Secret.");
                builder = builder.WithClientSecret(adOptions.ClientSecret);
            }
            else
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "YouHaveIssues", validOnly: false);
                var certificate = certs.Cast<X509Certificate2>().FirstOrDefault();
                if (certificate != null)
                {
                    _logger.LogInformation("Authenticating using Certificate '{Thumbprint}'", certificate.Thumbprint);
                    builder = builder.WithCertificate(certificate);
                }
                else
                {
                    _logger.LogError("No ClientSecret or Certificate configured");
                }
            }

            _app = builder.Build();

            var resource = kustoOptions.Value.ClusterUrl ?? throw new ArgumentException("Missing required configuration option: 'Kusto:ClusterUrl'");
            _scopes = new[] { $"{resource}/.default" };
        }

        public ValueTask<AuthenticationResult> AcquireTokenAsync()
        {
            var token = _token;
            if (token != null && token.ExpiresOn >= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return new ValueTask<AuthenticationResult>(token);
            }
            return AcquireTokenAsyncWithAwait();

            async ValueTask<AuthenticationResult> AcquireTokenAsyncWithAwait()
            {
                await _acquireTokenLock.WaitAsync();
                try
                {
                    // Recheck the token in the lock
                    token = _token;
                    if (token != null && token.ExpiresOn >= DateTimeOffset.UtcNow.AddMinutes(5))
                    {
                        return token;
                    }

                    // Acquire a new token
                    _logger.LogDebug("Acquiring new authentication token...");
                    _token = await _app.AcquireTokenForClient(_scopes)
                        .ExecuteAsync();
                    _logger.LogDebug("Token acquired.");

                    return _token;
                }
                finally
                {
                    _acquireTokenLock.Release();
                }
            }
        }

    }
}