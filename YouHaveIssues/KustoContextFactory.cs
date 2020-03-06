using Kusto.Cloud.Platform.Utils;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace YouHaveIssues
{
    public class KustoContextFactory
    {
        readonly AzureAuthenticationService _azureAuth;
        readonly IOptions<KustoOptions> _options;
        readonly ILoggerFactory _loggerFactory;

        public KustoContextFactory(AzureAuthenticationService azureAuth, IOptions<KustoOptions> options, ILoggerFactory loggerFactory)
        {
            _azureAuth = azureAuth;
            _options = options;
            _loggerFactory = loggerFactory;
        }

        public async ValueTask<KustoContext> CreateContextAsync()
        {
            var clusterUrl = _options.Value.ClusterUrl ?? throw new ArgumentException("Missing required configuration value: 'Kusto.ClusterUrl'");
            var databaseName = _options.Value.DatabaseName ?? throw new ArgumentException("Missing required configuration value: 'Kusto.DatabaseName'");

            var token = await _azureAuth.AcquireTokenAsync();
            var kcsb = new KustoConnectionStringBuilder(clusterUrl, databaseName)
                .WithAadUserTokenAuthentication(token.AccessToken);
            return new KustoContext(
                KustoClientFactory.CreateCslQueryProvider(kcsb),
                KustoClientFactory.CreateCslAdminProvider(kcsb),
                databaseName,
                _loggerFactory.CreateLogger<KustoContext>());
        }
    }
}
