using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace YouHaveIssues.Data
{
    public class Repository : IEnumerable<Area>
    {
        private readonly Lazy<IEnumerable<string>> allAreas;

        public Exception? Exception { get; }

        public string Organization { get; }

        public string Name { get; }

        public IList<Milestone> Milestones { get; }
        public DateTimeOffset RefreshTime { get; }

        public IEnumerable<string> AllAreas => allAreas.Value;

        public Repository(string organization, string name, IList<Milestone> milestones, DateTimeOffset refreshTime)
        {
            Organization = organization;
            Name = name;
            Milestones = milestones;
            RefreshTime = refreshTime;
            allAreas = new Lazy<IEnumerable<string>>(() =>
                new HashSet<string>(Milestones.SelectMany(m => m.Areas.Keys).OrderBy(a => a).ToList()));
        }

        public Repository(string organization, string name, Exception exception)
        {
            Organization = organization;
            Name = name;
            Exception = exception;

            Milestones = Array.Empty<Milestone>();
            allAreas = new Lazy<IEnumerable<string>>(() => Enumerable.Empty<string>());
        }

        public IEnumerator<Area> GetEnumerator()
        {
            var allArea = new Area("All");
            foreach (var m in Milestones)
            {
                allArea.Queries.Add(new Query(m.Name, m.TotalCount));
            }

            yield return allArea;

            foreach (var areaName in AllAreas)
            {
                var area = GetQueriesForArea(areaName);
                yield return area;
            }
        }

        private Area GetQueriesForArea(string areaName)
        {
            var area = new Area(areaName);
            foreach (var m in Milestones)
            {
                if (!m.Areas.TryGetValue(areaName, out var query))
                {
                    query = new Query(m.Name, 0);
                }
                else
                {
                    query = new Query(m.Name, query.Count);
                }

                area.Queries.Add(query);
            }

            return area;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class Area
    {
        public Area(string name)
            => this.Name = name;

        public string Name { get; }

        public IList<Query> Queries { get; } = new List<Query>();

        public Query this[string milestoneName]
            => Queries.Single(q => q.Name == milestoneName);

        public Query Get(string milestoneName)
            => this[milestoneName];
    }
}
