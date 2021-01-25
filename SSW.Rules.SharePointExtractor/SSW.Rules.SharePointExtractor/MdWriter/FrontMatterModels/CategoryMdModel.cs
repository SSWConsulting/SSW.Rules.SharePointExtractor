using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.MdWriter.FrontMatterModels
{
    public class CategoryMdModel
    {

        public CategoryMdModel(Category cat)
        {
            Title = cat.Title;
            Guid = cat.PageGuid ?? cat.TermStoreGuid;
            Uri = cat.Name.ToFileName();
            Index = cat.Rules.Select(r => r.GetRuleUri()).ToList();
        }

        public String Type => "category";

        public string Title { get; }

        public string Guid { get; }

        public string Uri { get;  }

        public List<string> Index { get; }

    }
}
