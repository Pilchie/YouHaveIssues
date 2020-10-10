using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace YouHaveIssues.Data
{
    public class AreaStatsByWeek
    {
        private readonly RepositoryConfig repositoryConfig;

        public AreaStatsByWeek(RepositoryConfig repositoryConfig)
        {
            this.repositoryConfig = repositoryConfig;
        }

        public async Task<Timeline> GetStats(KustoContext context, string organization, string name)
        {
            var timeline = await context.IssueTimeline(organization, name, repositoryConfig.GetAreaPrefixFor(organization, name));
            return timeline;
        }

        public static Timeline FromReader(IDataReader reader)
        {
            var issueUpdates = new Dictionary<long, List<TimelineRow>>();
            while (reader.Read())
            {
                // var org = reader.GetString(0);
                // var repo = reader.GetString(1);
                var state = reader.GetString(2);
                var number = reader.GetInt64(3);
                // var milestoneId = reader.GetValue(4);
                // var title = reader.GetString(5);
                // 6 - IssueId
                var updatedAt = reader.GetDateTime(7);
                // var createdAt = reader.GetDateTime(8);
                //var closedAt = reader.IsDBNull(9)
                //    ? (DateTime?)null
                //    : reader.GetDateTime(9);
                // 10 - labels
                // 11 - Data
                // 12 - Action
                // 13 - labels1
                // 14 - labelname
                var area = reader.GetString(15);
                // 15 - milestone
                var milestone = reader.GetString(17);

                issueUpdates.GetOrNew(number).Add(new TimelineRow(updatedAt, state, milestone, area));
            }

            var timeline = new Timeline();
            foreach (var (number, rows) in issueUpdates)
            {
                var first = rows.First();

                var currentState = first.State;
                var currentArea = first.Area;
                var currentMilestone = first.Milestone;
                timeline.AllAreas.Add(currentArea);
                timeline.AllMilestones.Add(currentMilestone);

                foreach (var row in rows.Skip(1))
                {
                    if (row.State != currentState)
                    {
                        if (row.State == "open")
                        {
                            timeline.AreaAndMilestone.GetOrNew((row.Area, row.Milestone)).Opened++;
                        }
                        else if (row.State == "closed")
                        {
                            timeline.AreaAndMilestone.GetOrNew((row.Area, row.Milestone)).Closed++;
                        }
                        else
                        {
                            throw new NotSupportedException("Unknown state");
                        }
                    }

                    if (row.Area != currentArea)
                    {
                        timeline.AreaAndMilestone.GetOrNew((currentArea, currentMilestone)).Left++;
                        timeline.AreaAndMilestone.GetOrNew((row.Area, row.Milestone)).Entered++;
                    }

                    if (row.Milestone != currentMilestone)
                    {
                        timeline.AreaAndMilestone.GetOrNew((currentArea, currentMilestone)).Left++;
                        timeline.AreaAndMilestone.GetOrNew((row.Area, row.Milestone)).Entered++;
                    }

                    currentState = row.State;
                    currentArea = row.Area;
                    currentMilestone = row.Milestone;
                    timeline.AllAreas.Add(currentArea);
                    timeline.AllMilestones.Add(currentMilestone);
                }
            }

            return timeline;
        }

        private class TimelineRow
        {
            public TimelineRow(DateTime updatedAt, string state, string milestone, string area)
            {
                UpdatedAt = updatedAt;
                State = state;
                Milestone = milestone;
                Area = area;
            }

            public DateTime UpdatedAt { get; }
            public string State { get; }
            public string Milestone { get; }
            public string Area { get; }
        }
    }

    public static class Extensions
    {
        public static TValue GetOrNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : notnull
            where TValue : new()
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }
    }

    public class Stats
    {
        public int Entered { get; set; }
        public int Left { get; set; }
        public int Closed { get; set; }
        public int Opened { get; internal set; }
    }

    public class Timeline
    {
        //public IDictionary<string, Stats> Milestones { get; } = new Dictionary<string, Stats>();
        //public IDictionary<string, Stats> Areas { get; } = new Dictionary<string, Stats>();
        public IDictionary<(string Area, string Milestone), Stats> AreaAndMilestone { get; } = new Dictionary<(string, string), Stats>();
        public HashSet<string> AllAreas { get; } = new HashSet<string>();
        public HashSet<string> AllMilestones { get; } = new HashSet<string>();
    }
}
