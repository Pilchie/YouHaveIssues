using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Azure.Cosmos.Table;
using Octokit;

namespace YouHaveIssues.Shared
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public class IssueEntity : TableEntity
    {
        public IssueEntity()
        { }

        public IssueEntity(Issue issue)
            : base(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), issue.Number.ToString("D"))
        {
            this.Milestone = issue.Milestone?.Title ?? "";
            this.Labels = string.Join(";", issue.Labels.Select(l => l.Name).ToArray());
            this.Assignees = string.Join(";", issue.Assignees.Select(a => a.Login).ToArray());
            this.Owner = issue.User.Login;
            this.Title = issue.Title;
        }

        public DateTime Date
            => DateTime.ParseExact(PartitionKey, "yyyy-MM-dd", null);

        public int Number
            => int.Parse(RowKey);

        public string? Milestone { get; set; }
        public string? Labels { get; set; }
        public string? Assignees { get; set; }
        public string? Owner { get; set; }
        public string? Title { get; set; }

        private string GetDebuggerDisplay()
            => $"{Number} - {Title?.Substring(0, 20)}";
    }
}
