using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubDocs.Models
{
    public class Page
    {
        public string CurrentBranch { get; set; }

        public IList<string> Branches { get; set; }

        public string Toc { get; set; }

        public string Content { get; set; }

        public string Endpoint { get; set; }

        public Contribution Contribution { get; set; }
    }
}
