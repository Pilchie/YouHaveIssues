using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace IssueDashboard.Data
{
    public class Repository
    {
        public string Organization { get; }

        public string Name { get; }

        public IList<Milestone> Milestones { get; }

        public Repository(string organization, string name, IList<Milestone> milestones)
        {
            Organization = organization;
            Name = name;
            this.Milestones = milestones;
        }
    }
}
