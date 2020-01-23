﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YouHaveIssues.Data
{
    public class Milestone
    {
        public string Name { get; }

        public IDictionary<string, Query> Areas { get; } = new Dictionary<string, Query>();

        public int TotalCount { get; set; }

        public Milestone(string name)
        {
            this.Name = name;
        }
    }
}