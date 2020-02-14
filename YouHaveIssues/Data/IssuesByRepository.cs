using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace YouHaveIssues.Data
{
    public class IssuesByRepository
    {
        public const string NoMilestone = "(No Milestone)";
        public const string NoArea = "(No Area)";

        private static readonly (string org, string name)[] KnownRepos = 
        {
            ("dotnet", "aspnetcore"),
            ("dotnet", "extensions"),
            ("dotnet", "efcore"),
            ("dotnet", "ef6"),
        };

        private readonly GitHubClient gitHubClient;

        private Dictionary<(string org, string repo), (DateTimeOffset time, Task<Repository> repo)> cache = new Dictionary<(string org, string repo), (DateTimeOffset time, Task<Repository> repo)>();

        public IssuesByRepository(GitHubClient gitHubClient)
        {
            this.gitHubClient = gitHubClient;
            var _ = UpdateCachePeriodically();
        }

        private async Task UpdateCachePeriodically()
        {
            var tasks = new List<Task>(KnownRepos.Length + 1);
            while (true)
            {
                foreach (var repo in KnownRepos)
                {
                    tasks.Add(GetRepository(repo.org, repo.name));
                }

                tasks.Add(Task.Delay(TimeSpan.FromMinutes(15)));
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        public async Task<Repository> GetRepository(string organization, string name)
        {
            var key = (organization, name);
            Task<Repository> repoTask;
            lock (this.cache)
            {
                if (!cache.TryGetValue(key, out var value) ||
                    value.repo.IsFaulted ||
                    value.repo.IsCanceled ||
                    (value.repo.IsCompletedSuccessfully && value.repo.Result.Exception != null) ||
                    DateTimeOffset.UtcNow - value.time > TimeSpan.FromMinutes(15))
                {
                    value.time = DateTimeOffset.UtcNow;
                    value.repo = GetRepositoryCore(organization, name, value.time);
                    cache[key] = value;
                }

                repoTask = value.repo;
            }

            return await repoTask;
        }

        private async Task<Repository> GetRepositoryCore(string organization, string name, DateTimeOffset refreshTime)
        {
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
            };

            var milestones = new Dictionary<string, Milestone>();
            IReadOnlyList<Issue> issues = Array.Empty<Issue>();
            RateLimit rateLimit;
            try
            {
                issues = await gitHubClient.Issue.GetAllForRepository(organization, name, issueRequest);
                var rateLimits = await gitHubClient.Miscellaneous.GetRateLimits();
                rateLimit = rateLimits.Resources.Core;
            }
            catch (Exception e)
            {
                return new Repository(organization, name, e);
            }

            foreach (var issue in issues)
            {
                if (issue.PullRequest != null)
                {
                    continue;
                }

                var key = NoMilestone;
                if (issue.Milestone != null)
                {
                    key = issue.Milestone.Title;
                }

                if (!milestones.TryGetValue(key, out var milestone))
                {
                    milestone = new Milestone(key);
                    milestones[key] = milestone;
                }

                milestone.TotalCount++;

                var areas = issue.Labels.Select(l => l.Name).Where(l => l.StartsWith("area-")).ToList();
                if (!areas.Any())
                {
                    areas.Add(NoArea);
                }

                foreach (var area in areas)
                {
                    if (!milestone.Areas.TryGetValue(area, out var query))
                    {
                        query = new Query(area);
                        milestone.Areas[area] = query;
                    }

                    query.Issues.Add(issue);
                }
            }

            return new Data.Repository(
                organization, name, milestones.Values.OrderBy(m => m.Name).ToList(), rateLimit, refreshTime);
        }
    }
}
