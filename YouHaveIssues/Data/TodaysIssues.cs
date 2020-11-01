using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using YouHaveIssues.Shared;

namespace YouHaveIssues.Data
{
    public class TodaysIssues
    {
        private TodaysIssues(HashSet<string> allAreas, HashSet<string> allMilestones, HashSet<string> allAssignees, IEnumerable<IssueEntity> issues)
        {
            AllAreas = allAreas;
            AllMilestones = allMilestones;
            AllAssignees = allAssignees;
            Issues = issues.ToList();
        }

        public HashSet<string> AllAreas { get; }
        public HashSet<string> AllMilestones { get; }
        public HashSet<string> AllAssignees { get; }
        public IEnumerable<IssueEntity> Issues { get; }

        public static TodaysIssues FetchFromTableStorage(string connectionString)
        {
            return FetchFromTableStorage(connectionString, DateTime.UtcNow.Date);
        }

        public static TodaysIssues FetchFromTableStorage(string connectionString, DateTime date)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("dotnet-aspnetcore");
            var todayString = date.ToString("yyyy-MM-dd");
            var query = new TableQuery<IssueEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, todayString));
            var issues = table.ExecuteQuery(query);
            var allAreas = new HashSet<string>(StringComparer.Ordinal);
            var allMilestones = new HashSet<string>(StringComparer.Ordinal);
            var allAssignees = new HashSet<string>(StringComparer.Ordinal);
            foreach (var issue in issues)
            {
                foreach (var label in issue.Labels?.Split(";", StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                {
                    if (label.StartsWith("area-", StringComparison.Ordinal))
                    {
                        allAreas.Add(label.Substring("area-".Length));
                    }
                }

                allMilestones.Add(issue.Milestone ?? "");

                foreach (var assignee in issue.Assignees?.Split(";", StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                {
                    allAssignees.Add(assignee);
                }

            }

            return new TodaysIssues(allAreas, allMilestones, allAssignees, issues);
        }
    }
}
