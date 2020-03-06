using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace YouHaveIssues.Data
{
    public class IssuesByRepository
    {
        public const string NoMilestone = "(No Milestone)";
        public const string NoArea = "(No Area)";

        private readonly RepositoryConfig repositoryConfig;

        public IssuesByRepository(RepositoryConfig repositoryConfig)
        {
            this.repositoryConfig = repositoryConfig;
        }

        public async Task<Repository> GetRepository(KustoContext context, string organization, string name)
        {
            var repos = await context.IssuesByMilestoneAndArea(organization, name, repositoryConfig.GetAreaPrefixFor(organization, name));
            return repos.First();
        }

        public static IReadOnlyList<Repository> FromReader(string organization, IDataReader reader)
        {
            var milestones = new Dictionary<string, Milestone>();
            string? repository = null;
            using (reader)
            {
                while (reader.Read())
                {
                    var repo = reader.GetString(0);
                    var milestoneName = reader.GetString(1);
                    var area = reader.GetString(2);
                    var count = (int)reader.GetInt64(3);

                    repository ??= repo;
                    if (repo != repository)
                    {
                        throw new NotSupportedException($"Got results for an unexpected repository.  Expected '{repository}', but got '{repo}'");
                    }

                    if (!milestones.TryGetValue(milestoneName, out var milestone))
                    {
                        milestone = new Milestone(milestoneName);
                        milestones[milestoneName] = milestone;
                    }

                    milestone.TotalCount += count;
                    milestone.Areas[area] = new Query(area, count);
                }
            }

            return new[] { new Data.Repository(organization, repository ?? "", milestones.Values.OrderBy(m => m.Name).ToList(), DateTimeOffset.UtcNow) };
        }
    }
}
