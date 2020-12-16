using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.MdWriter.FrontMatterModels
{
    public class RuleMdModel
    {
        public RuleMdModel(RulePage rule)
        {
            Title = rule.Title;
            Created = rule.CreatedUtc;
            Guid = rule.Guid;
            Uri = rule.Name.ToFileName();
            Authors = rule.Employees.Select(e => new AuthorMdModel(e)).ToList();
            ArchivedReason = rule.ArchivedReason;
            Related = rule.Related.ToList();
        }

        public string Type => "rule";

        public string ArchivedReason { get; }

        public string Title { get; }

        public string Guid { get; }

        public string Uri { get; }

        public DateTime Created { get; }

        public List<AuthorMdModel> Authors { get; }

        public List<string> Related { get; }
    }

    public class AuthorMdModel
    {
        public AuthorMdModel(Employee e)
        {
            Id = e.Id;
            Title = e.Title;
        }

        public int Id { get; }
        public string Title { get; }
    }
}

