﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IssueDashboard.Data
{
    public class Milestone
    {
        public string Name { get; }

        public IDictionary<string, Query> Areas { get; } = new Dictionary<string, Query>();

        public int TotalCount => Areas.Values.Sum(a => a.Count);

        public Milestone(string name)
        {
            this.Name = name;
        }
    }
}
