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
        private string _gitHubToken;
        private string _connectionString;

        public UpdateIssuesTable(IConfiguration configuration)
        {
            _gitHubToken = configuration["GitHubToken"];
            _connectionString = configuration["ConnectionString"];

        }

        [FunctionName("UpdateIssuesTable")]
        public async Task Run([TimerTrigger("0 0 5 * * * ")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var ghc = new GitHubClient(new Connection(
                        new ProductHeaderValue("YouHaveIssues")))
            {
                Credentials = new Credentials(_gitHubToken),
            };
            var client = ghc.Issue;
            var issues = await client.GetAllForRepository("dotnet", "aspnetcore", new RepositoryIssueRequest { State = ItemStateFilter.Open, });

            var storageAccount = CloudStorageAccount.Parse(_connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("dotnet-aspnetcore");
            if (!await table.CreateIfNotExistsAsync())
            {
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
            }
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
