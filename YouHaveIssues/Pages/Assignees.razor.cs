using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using YouHaveIssues.Data;
using YouHaveIssues.Shared;

namespace YouHaveIssues.Pages
{
    public partial class Assignees
    {
        [Parameter] public string? Organization { get; set; }
        [Parameter] public string? Name { get; set; }

        string[]? Milestones { get; set; }
        string[]? Areas { get; set; }
        IssueEntity[]? Issues;
        private IEnumerable<string>? _currentMilestones;
        private IEnumerable<string>? _currentAreas;

        AssigneeCount[]? Counts { get; set; }

        protected override void OnParametersSet()
        {
            var connectionString = Configuration["ConnectionString"];
            var todaysIssues = TodaysIssues.FetchFromTableStorage(connectionString);
            Milestones = todaysIssues.AllMilestones.OrderBy(m => m).ToArray();
            Areas = todaysIssues.AllAreas.OrderBy(a => a).ToArray();
            Issues = todaysIssues.Issues.ToArray();
            UpdateCounts();
        }

        void MilestoneChanged(object o)
        {
            UpdateCounts();
        }

        void AreaChanged(object o)
        {
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            if (Issues == null)
            {
                return;
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var i in Issues)
            {
                if (_currentAreas != null && !_currentAreas.Any(a => i.Labels.Contains(a)))
                {
                    continue;
                }

                if (_currentMilestones != null && !_currentMilestones.Contains(i.Milestone))
                {
                    continue;
                }

                foreach (var a in i.Assignees.Split(";", StringSplitOptions.None))
                {
                    var assigneee = a;
                    if (assigneee == "")
                    {
                        assigneee = "(unassigned)";
                    }
                    if (!counts.TryGetValue(assigneee, out var c))
                    {
                        c = 0;
                    }
                    counts[assigneee] = ++c;
                }
            }

            Counts = counts.Select(kvp => new AssigneeCount(kvp.Key, kvp.Value)).ToArray();
        }

        class AssigneeCount
        {
            public string Login { get; }
            public int Count { get; }
            public AssigneeCount(string login, int count)
            {
                Login = login;
                Count = count;
            }
        }
    }
}
