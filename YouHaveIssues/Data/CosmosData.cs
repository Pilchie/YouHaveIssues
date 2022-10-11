using Kusto.Cloud.Platform.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace YouHaveIssues.Data;

public class CosmosRepository
{
    public string Name { get; init; } = "";
    public long Issues { get; init; }
    public long PullRequests { get; init; }
}

public class CosmosData
{
    private CosmosData(IEnumerable<CosmosRepository> repositories)
        => Repositories = repositories.ToList();

    private CosmosData(Exception exception)
    {
        Exception = exception;
        Repositories = Array.Empty<CosmosRepository>();
    }

    public static CosmosData FromReader(IDataReader reader)
    {
        var repositories = new List<CosmosRepository>();
        using (reader)
        {
            while (reader.Read())
            {
                var repo = reader.GetString(0);
                var issues = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                var prs = reader.IsDBNull(2) ? 0 : reader.GetInt64(2);
                repositories.Add(new CosmosRepository
                {
                    Name = repo,
                    Issues = issues,
                    PullRequests = prs,
                });
            }
        }

        return new(repositories);
    }

    public IReadOnlyList<CosmosRepository> Repositories { get; }

    public Exception? Exception { get; }

}
