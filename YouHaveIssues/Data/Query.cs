using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace YouHaveIssues.Data
{
    public class Query
    {
        public string Name { get; }

        public int Count => Issues.Count;

        public IList<Issue> Issues { get; } = new List<Issue>();

        public Query(string name)
        {
            Name = name;
        }
    }
}
