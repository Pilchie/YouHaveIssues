using Kusto.Data.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YouHaveIssues.Data;

namespace YouHaveIssues
{
    public class KustoContext
    {
        private static readonly ConcurrentDictionary<string, string> _queryCache = new ConcurrentDictionary<string, string>();
        private readonly ICslQueryProvider _query;
        private readonly ICslAdminProvider _admin;
        private readonly string _databaseName;
        private readonly ILogger _logger;

        public KustoContext(ICslQueryProvider query, ICslAdminProvider admin, string databaseName, ILogger logger)
        {
            _query = query;
            _admin = admin;
            _databaseName = databaseName;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Repository>> IssuesByMilestoneAndArea(string organization, string repository, string areaPrefix, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Sending Query: {nameof(IssuesByMilestoneAndArea)}, ([{{Organization}}], [{{Repository}}])", organization, repository);
            var reader = await ExecuteQueryAsync(
                GetQuery(nameof(IssuesByMilestoneAndArea)),
                cancellationToken,
                ("Organization", organization),
                ("Repository", repository),
                ("AreaPrefix", areaPrefix));
            _logger.LogDebug($"Query Complete: {nameof(IssuesByMilestoneAndArea)}, ([{{Organization}}], [{{Repository}}])", organization, repository);

            return IssuesByRepository.FromReader(organization, reader);
        }

        public async Task<CosmosData> GetCosmosData(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Sending Query: {nameof(GetCosmosData)}");
            var reader = await ExecuteQueryAsync(
            GetQuery("CosmosDB"),
                cancellationToken);
            _logger.LogDebug($"Query Complete: {nameof(GetCosmosData)}");
            return CosmosData.FromReader(reader);
        }

        public async Task<Timeline> IssueTimeline(string organization, string repository, string areaPrefix, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"Sending Query: {nameof(IssueTimeline)}, ([{{Organization}}], [{{Repository}}])", organization, repository);
            var reader = await ExecuteQueryAsync(
                GetQuery(nameof(IssueTimeline)),
                cancellationToken,
                ("Organization", organization),
                ("Repository", repository),
                ("AreaPrefix", areaPrefix));
            _logger.LogDebug($"Query Complete: {nameof(IssueTimeline)}, ([{{Organization}}], [{{Repository}}])", organization, repository);

            return AreaStatsByWeek.FromReader(reader);
        }

        private async Task<IDataReader> ExecuteQueryAsync(string query, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            // Yes yes, I know. We log things here though and can't return a task :(.
            static async void OnCancellation((KustoContext Context, string RequestId) state)
            {
                try
                {
                    state.Context._logger.LogDebug("Attempting to cancel query '{RequestId}'", state.RequestId);
                    await state.Context._admin.ExecuteControlCommandAsync(state.Context._databaseName, $".cancel query {state.RequestId}");
                    state.Context._logger.LogDebug("Cancelled query '{RequestId}'", state.RequestId);
                }
                catch (Exception ex)
                {
                    state.Context._logger.LogError(ex, "Error cancelling query '{RequestId}'", state.RequestId);
                }
            }

            static void ApplyParameter(ClientRequestProperties properties, string name, object? value)
            {
                switch (value)
                {
                    case null:
                        break;
                    case string sv:
                        properties.SetParameter(name, sv);
                        break;
                    case DateTime dtv:
                        properties.SetParameter(name, dtv);
                        break;
                    case TimeSpan tsv:
                        properties.SetParameter(name, tsv);
                        break;
                    case bool bv:
                        properties.SetParameter(name, bv);
                        break;
                    case int iv:
                        properties.SetParameter(name, iv);
                        break;
                    case long lv:
                        properties.SetParameter(name, lv);
                        break;
                    case Guid gv:
                        properties.SetParameter(name, gv);
                        break;
                    case double dv:
                        properties.SetParameter(name, dv);
                        break;
                    default:
                        throw new NotSupportedException($"Parameter type not supported: {value.GetType().FullName}");
                }
            }

            var requestId = $"YouHaveIssues;{Guid.NewGuid():N}";
            var properties = new ClientRequestProperties()
            {
                ClientRequestId = requestId
            };

            foreach (var (Name, Value) in parameters)
            {
                ApplyParameter(properties, Name, Value);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var paramsString = string.Join(",", parameters.Select(p => $"{p.Name}=[{p.Value}]"));
                _logger.LogTrace("Executing request {RequestId}. Parameters ({Parameters}). Query: {Query}", requestId, paramsString, query);
            }

            using var registration = cancellationToken.CanBeCanceled ? cancellationToken.Register(OnCancellation, (this, requestId)) : null;
            var reader = await _query.ExecuteQueryAsync(_databaseName, query, properties);
            return reader;
        }

        private string GetQuery(string name)
        {
            return _queryCache.GetOrAdd(name, n =>
            {
                var queryResourceName = $"{typeof(Program).Namespace}.Queries.{n}.kql";
                using var stream = typeof(KustoContext).Assembly.GetManifestResourceStream(queryResourceName);
                using var reader = new StreamReader(stream ?? throw new KeyNotFoundException($"Query '{queryResourceName}' does not exist!"));
                return reader.ReadToEnd().Trim();
            });
        }
    }
}




