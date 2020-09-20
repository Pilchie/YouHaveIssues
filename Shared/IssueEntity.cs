using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Cosmos.Table;
using Octokit;

namespace YouHaveIssues.Shared
{
    public class IssueEntity : TableEntity
    {
        public IssueEntity()
        { }

        public IssueEntity(Issue issue)
            : base(DateTime.UtcNow.Date.ToString("yyyy-mm-dd"), issue.Number.ToString("D"))
        {
            this.Milestone = issue.Milestone?.Title ?? "";
            this.Labels = string.Join(";", issue.Labels.Select(l => l.Name).ToArray());
            this.Assignees = string.Join(";", issue.Assignees.Select(a => a.Login).ToArray());
            this.Owner = issue.User.Login;
        }

        public DateTime Date
            => DateTime.ParseExact(PartitionKey, "yyyy-mm-dd", null);

        public int Number
            => int.Parse(RowKey);

        public string Milestone { get; set; }
        public string Labels { get; set; }
        public string Assignees { get; set; }
        public string Owner { get; set; }
    }
}
