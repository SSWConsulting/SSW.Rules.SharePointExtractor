using System;
using System.Collections.Generic;

namespace SSW.Rules.SharePointExtractor.Models
{
    public class Category: IHasContentHistory
    {
        public string PageGuid { get; set; }

        public int TermStoreId { get; set; }
        
        public string TermStoreGuid { get; set; }

        public string ParentCategoryTitle { get; set; }

        public string Title { get; set; }

        public string Name => Title?.ToLowerInvariant().Replace(' ', '-');
      
        public Uri Uri { get; set; }

        public string Content { get; set; }

        public string IntroText { get; set; }


        public IList<RulePage> Rules { get; set; } = new List<RulePage>();

        public HashSet<string> ImageUrls { get; set; } = new HashSet<string>();

        public IList<ContentVersion> Versions { get; set; } = new List<ContentVersion>();
    }
}