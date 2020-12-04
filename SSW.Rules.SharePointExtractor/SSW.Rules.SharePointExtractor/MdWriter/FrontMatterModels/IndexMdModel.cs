using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.MdWriter.FrontMatterModels
{
    public class IndexMdModel
    {
        public IndexMdModel(SpRulesDataSet data)
        {
            Index = data.ParentCategories.Select(pc => pc.Title.ToFileName()).ToList();
        }

        public string Type => "main";

        public List<string> Index { get; }

    }
}
