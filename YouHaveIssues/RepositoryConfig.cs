using System.Collections.Generic;

namespace YouHaveIssues
{
    public class RepositoryConfig
    {
        public IDictionary<string, RepositoryOptions> Repositories { get; } = new Dictionary<string, RepositoryOptions>();

        public string GetAreaPrefixFor(string organization, string repo)
        {
            Repositories.TryGetValue($"{organization}/{repo}", out var repoConfig);
            return repoConfig?.AreaPrefix ?? "area-";
        }
    }

    public class RepositoryOptions
    {
        public string? AreaPrefix { get; set; }
    }
}