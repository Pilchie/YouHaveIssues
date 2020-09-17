using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using YouHaveIssues.Shared;

[assembly: FunctionsStartup(typeof(UpdateTrendsTable.Startup))]

namespace UpdateTrendsTable
{

    public class UpdateIssuesTable
    {
        private readonly IConfiguration _configuration;

        public UpdateIssuesTable(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("UpdateIssueTableOnDemand")]
        [NoAutomaticTrigger]
        public Task RunOnDemand(ILogger log)
        {
            return RunTableUpdate(log);
        }

        [FunctionName("UpdateIssuesTableOnTimer")]
        public Task RunOnTimer([TimerTrigger("0 15 21 * * *")] TimerInfo myTimer, ILogger log)
        {
            return RunTableUpdate(log);
        }

        private async Task RunTableUpdate(ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var gitHubToken = _configuration["GitHubToken"];
            if (string.IsNullOrEmpty(gitHubToken))
            {
                log.LogError($"No GitHubToken configured");
                return;
            }

            var connectionString = _configuration["ConnectionString"];
            if (string.IsNullOrEmpty(connectionString))
            {
                log.LogError($"No ConnectionString configured");
                return;
            }

            var ghc = new GitHubClient(new Connection(
                        new ProductHeaderValue("YouHaveIssues")))
            {
                Credentials = new Credentials(gitHubToken),
            };
            var client = ghc.Issue;
            var issues = await client.GetAllForRepository("dotnet", "aspnetcore", new RepositoryIssueRequest { State = ItemStateFilter.Open, });
            log.LogInformation($"Got {issues.Count} issues back from GitHub");

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("dotnet-aspnetcore");
            if (!await table.CreateIfNotExistsAsync())
            {
                log.LogInformation("Created table");
            }

            var index = 0;
            while (index < issues.Count)
            {
                var batch = new TableBatchOperation();
                while (batch.Count < 100 && index < issues.Count)
                {
                    if (issues[index].PullRequest == null)
                    {
                        batch.Insert(new IssueEntity(issues[index]));
                    }
                    index++;
                }
                var result = await table.ExecuteBatchAsync(batch);
                log.LogInformation($"Executed batch insert of {batch.Count} items.");
            }

            log.LogInformation($"Done execution.");
        }
    }

    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(Environment.CurrentDirectory)
               .AddJsonFile("local.settings.json", true)
               .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
               .AddEnvironmentVariables()
               .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
        }
    }
}
