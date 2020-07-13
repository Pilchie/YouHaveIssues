namespace YouHaveIssues.Data
{
    public class Query
    {
        public string Name { get; }

        public int Count { get; }

        public Query(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }
}
