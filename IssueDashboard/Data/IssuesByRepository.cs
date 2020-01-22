using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace IssueDashboard.Data
{
    public class IssuesByRepository
    {
        private readonly GitHubClient gitHubClient;

        public IssuesByRepository(GitHubClient gitHubClient)
        {
            this.gitHubClient = gitHubClient;
        }

        public async Task<Data.Repository> GetRepository(string organization, string name)
        {
            var issueRequest = new RepositoryIssueRequest
            {
                State = ItemStateFilter.Open,
            };

            var milestones = new Dictionary<string, Milestone>();
            var issues = await gitHubClient.Issue.GetAllForRepository(organization, name, issueRequest);
            foreach (var issue in issues)
            {
                var key = "(No Milestone)";
                if (issue.Milestone != null)
                {
                    key = issue.Milestone.Title;
                }

                if (!milestones.TryGetValue(key, out var milestone))
                {
                    milestone = new Milestone(key);
                    milestones[key] = milestone;
                }

                var areas = issue.Labels.Select(l => l.Name).Where(l => l.StartsWith("area-")).ToList();
                if (!areas.Any())
                {
                    areas.Add("(No Area)");
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
