using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace SSW.Rules.SharePointExtractor.Models
{
    public class RulePage: IHasContentHistory
    {
        public int Id { get; set; }
        

        public string Guid { get; set; }

        public string ArchivedReason { get; set; }

        public string IntroText { get; set; }

        public string Content { get; set; }

        public string Title { get; set; }

        public string Name => Title.ToLowerInvariant().Replace(' ', '-');

        public string RulesKeyWords { get; set; }

        public IList<Employee> Employees { get; set; } = new List<Employee>();

        public IList<string> Related { get; set; } = new List<string>();

        public IList<string> Redirects { get; set; } = new List<string>();

        public IList<Category> Categories { get; set; } = new List<Category>();

        public DateTime CreatedUtc { get; set; }
        
        public DateTime ModifiedUtc { get; set; }

        public IList<ContentVersion> Versions { get; set; } = new List<ContentVersion>();

        public HashSet<string> ImageUrls { get; set; } = new HashSet<string>();


    }


    public class ContentVersion
    {
        public string VersionLabel { get; set; }

        public string Comment  { get; set; }

        public DateTime ModifiedUtc { get; set; }

        public string ModifiedBy { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public string IntroText { get; set; }




        public string ModifiedByName => ModifiedByEmail.Substring(0, ModifiedByEmail.IndexOf("@", StringComparison.Ordinal));


        /// <summary>
        /// extract an email from the ModifiedBy value
        /// example values:#MartinHinshelwood@ssw.com.au, #
        /// </summary>
        public string ModifiedByEmail
        {
            get
            {
                // trim leading #
                var result = ModifiedBy.Substring(1);
                return string.IsNullOrEmpty(result) ? "Unknown@ssw.com.au" : result;
            }
        }

        public string ModifiedByFullName
        {
            get;set;
        }
        public string ModifiedByDisplayName
        {
            get
            {
                // trim leading #
                var result = ModifiedByFullName.Substring(1).Replace("www.ssw.com.au","").Trim();
                return string.IsNullOrEmpty(result) ? "Unknown" : result;
            }
        }
    }
    


}