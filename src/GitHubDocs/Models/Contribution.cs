using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubDocs.Models
{
    public class Contribution
    {
        public DateTime? LastUpdate { get; set; }

        public IDictionary<string, string> Contributors { get; set; } = new Dictionary<string, string>();
    }
}
