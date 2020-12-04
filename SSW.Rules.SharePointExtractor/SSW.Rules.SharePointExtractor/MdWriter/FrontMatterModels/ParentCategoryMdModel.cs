using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.MdWriter.FrontMatterModels
{
    public class ParentCategoryMdModel
    {

        public ParentCategoryMdModel(ParentCategory pc)
        {
            Title = pc.Title;
            Uri = pc.Title.ToFileName();
            Index = pc.Categories.Select(c => c.Name.ToFileName()).ToList();
        }

        public string Type => "top-category";

        public string Title { get; }

        public string Uri { get; } 

        public List<string> Index { get; }

    }
}
