using System.Collections.Generic;

namespace SSW.Rules.SharePointExtractor.Models
{
    public class ParentCategory
    {
        public string Title { get; set; }
       
        public IList<Category> Categories { get; set; } = new List<Category>();

    }
}