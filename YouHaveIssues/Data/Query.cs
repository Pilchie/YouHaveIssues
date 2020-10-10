using System.Diagnostics;

namespace YouHaveIssues.Data
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public class Query
    {
        public string Name { get; }

        public int Count { get; }

        public Query(string name, int count)
        {
            Name = name;
            Count = count;
        }

        private string GetDebuggerDisplay()
           => $"{Name} - {Count}";
    }
}
