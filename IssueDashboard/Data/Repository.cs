using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace IssueDashboard.Data
{
    public class Repository
    {
        public string Organization { get; }

        public string Name { get; }

        public IList<Milestone> Milestones { get; }

        public Repository(string organization, string name, GitHubClient gitHubClient)
        {
            Organization = organization;
            Name = name;

            var issueRequest = new RepositoryIssueRequest
            {
                Milestone = "*",
                State = ItemStateFilter.Open,
            };

            var milestones = new Dictionary<string, Milestone>();
            var issues = gitHubClient.Issue.GetAllForRepository(this.Organization, this.Name, issueRequest).Result;
            foreach (var issue in issues)
            {
                var key = "";
                if (issue.Milestone != null)
                {
                    key = issue.Milestone.Title;
                }

                if (!milestones.TryGetValue(key, out var milestone))
                {
                    milestone = new Milestone(key);
                    milestones[key] = milestone;
                }

                var areas = issue.Labels?.Select(l => l.Name).Where(l => l.StartsWith("area-")).ToList() ?? new List<string> { "" };
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

            this.Milestones = milestones.Values.ToList();
        }
    }
}
