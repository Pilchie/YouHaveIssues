using System.Collections.Generic;

namespace YouHaveIssues.Data
{
    public class Milestone
    {
        public string Name { get; }

        public IDictionary<string, Query> Areas { get; } = new Dictionary<string, Query>();

        public int TotalCount { get; set; }

        public Milestone(string name)
        {
            Name = name;
        }
    }
}
