using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using YouHaveIssues.Data;
using YouHaveIssues.Shared;

namespace YouHaveIssues.Pages
{
    public partial class TestFailures
    {
        public int TotalOpenCount { get; private set; }
        public int ClosedThisWeekCount { get; private set; }
        public int OpenedThisWeekCount { get; private set; }
        public IList<IssueEntity>? ThisWeek { get; private set; }
        public IList<IssueEntity>? LastWeek { get; private set; }

        public IList<Row>? GridData { get; private set; }

        protected override void OnInitialized()
        {
            ThisWeek = GetIssuesForDate(DateTime.UtcNow.Date);
            LastWeek = GetIssuesForDate(DateTime.UtcNow.AddDays(-7).Date);

            TotalOpenCount = ThisWeek.Count;
            ClosedThisWeekCount = LastWeek.Except(ThisWeek, IssueEntityComparer.Instance).Count();
            OpenedThisWeekCount = ThisWeek.Except(LastWeek, IssueEntityComparer.Instance).Count();

            GridData = new List<Row>(Math.Max(LastWeek.Count, ThisWeek.Count));

            if (LastWeek is object && ThisWeek is object)
            {
                int i = 0, j = 0;
                for (; i < LastWeek.Count && j < ThisWeek.Count;)
                {
                    if (LastWeek[i].Number == ThisWeek[j].Number)
                    {
                        GridData.Add(new Row(LastWeek[i++].Number, ThisWeek[j++].Number));
                    }
                    else if (LastWeek[i].Number < ThisWeek[j].Number)
                    {
                        GridData.Add(new Row(LastWeek[i++].Number, null));
                    }
                    else if (LastWeek[i].Number > ThisWeek[j].Number)
                    {
                        GridData.Add(new Row(null, ThisWeek[j++].Number));
                    }
                    else
                    {
                        throw new InvalidOperationException("Shouldn't Happen");
                    }
                }

                for (; i < LastWeek.Count;)
                {
                    GridData.Add(new Row(LastWeek[i++].Number, null));
                }
                for (; j < ThisWeek.Count;)
                {
                    GridData.Add(new Row(null, ThisWeek[j++].Number));
                }
            }
        }

        private IList<IssueEntity> GetIssuesForDate(DateTime date)
        {
            return new HashSet<IssueEntity>(
                TodaysIssues.FetchFromTableStorage(Configuration["ConnectionString"], date).Issues
                    .Where(issue => issue.Labels is object && issue.Labels.Contains("test-failure", StringComparison.Ordinal)), IssueEntityComparer.Instance)
                .OrderBy(ie => ie.Number)
                .ToList();
        }

        private class IssueEntityComparer : IEqualityComparer<IssueEntity>
        {
            private IssueEntityComparer() { }

            public static IssueEntityComparer Instance { get; } = new IssueEntityComparer();

            public bool Equals(IssueEntity? x, IssueEntity? y)
            {
                if (x is null && y is null)
                {
                    return true;
                }
                else if (x is null || y is null)
                {
                    return false;
                }

                return x.Number == y.Number;
            }

            public int GetHashCode(IssueEntity obj)
            {
                return obj.Number;
            }
        }
    }

    public class Row
    {
        public int? LastWeek { get; }
        public int? ThisWeek { get; }

        public Row(int? lastWeek, int? thisWeek)
        {
            LastWeek = lastWeek;
            ThisWeek = thisWeek;
        }
    }
}
