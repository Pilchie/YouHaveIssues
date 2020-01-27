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

        private readonly GitHubClient gitHubClient;

        private Dictionary<(string org, string repo), (DateTimeOffset time, Task<Repository> repo)> cache = new Dictionary<(string org, string repo), (DateTimeOffset time, Task<Repository> repo)>();

        public IssuesByRepository(GitHubClient gitHubClient)
        {
            this.gitHubClient = gitHubClient;
            Prepopulate();
        }

        private void Prepopulate()
        {
            foreach (var key in new(string org, string name)[] { ("dotnet", "aspnetcore"), ("dotnet", "extensions"), ("dotnet", "efcore"), ("dotnet", "ef6")})
            {
                cache[key] = (DateTimeOffset.UtcNow, GetRepositoryCore(key.org, key.name));
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
                    value.repo = GetRepositoryCore(organization, name);
                    value.time = DateTimeOffset.UtcNow;
                    cache[key] = value;
                }

                repoTask = value.repo;
            }

            return await repoTask;
        }

        private async Task<Repository> GetRepositoryCore(string organization, string name)
        {
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
            };

            var milestones = new Dictionary<string, Milestone>();
            IReadOnlyList<Issue> issues = Array.Empty<Issue>();
            try
            {
                issues = await gitHubClient.Issue.GetAllForRepository(organization, name, issueRequest);
            }
            catch (Exception e)
            {
                return new Repository(e);
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

            return new Data.Repository(organization, name, milestones.Values.OrderBy(m => m.Name).ToList());
        }
    }
}
