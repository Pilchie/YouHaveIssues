using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace YouHaveIssues.Data
{
    public class Repository
    {
        private readonly Lazy<IEnumerable<string>> allAreas;

        public Exception Exception { get; }

        public string Organization { get; }

        public string Name { get; }

        public IList<Milestone> Milestones { get; }
        public RateLimit RateLimit { get; }
        public DateTimeOffset RefreshTime { get; }

        public IEnumerable<string> AllAreas => allAreas.Value;

        public Repository(string organization, string name, IList<Milestone> milestones, RateLimit rateLimit, DateTimeOffset refreshTime)
        {
            Organization = organization;
            Name = name;
            Milestones = milestones;
            RateLimit = rateLimit;
            RefreshTime = refreshTime;
            allAreas = new Lazy<IEnumerable<string>>(() =>
                new HashSet<string>(Milestones.SelectMany(m => m.Areas.Keys).OrderBy(a => a).ToList()));
        }

        public Repository(Exception exception)
        {
            this.Exception = exception;
        }
    }
}
