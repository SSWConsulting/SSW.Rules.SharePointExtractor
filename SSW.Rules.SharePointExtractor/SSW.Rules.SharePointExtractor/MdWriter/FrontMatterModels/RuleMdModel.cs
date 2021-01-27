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
            Uri = rule.Name.CreateUriAndRedirect(rule);
            Authors = rule.Employees.Select(e => new AuthorMdModel(e)).ToList();
            ArchivedReason = rule.ArchivedReason;
            Related = rule.Related.ToList();
            Redirects = rule.Redirects.Distinct().Where(r => !r.ToLower().Equals(Uri)).ToList();
        }

        public string Type => "rule";

        public string ArchivedReason { get; }

        public string Title { get; }

        public string Guid { get; }

        public string Uri { get; }

        public DateTime Created { get; }

        public List<AuthorMdModel> Authors { get; }

        public List<string> Related { get; }

        public List<string> Redirects { get; }
    }

    public class AuthorMdModel
    {
        public AuthorMdModel(Employee e)
        {
            Title = e.Title;
            Url = "https://ssw.com.au/people/" + e.Title.ToLower().Replace(' ', '-');
        }

        public string Title { get; }
        public string Url { get; set; }
    }
}