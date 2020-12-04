using System.Collections.Generic;
using System.Linq;

namespace SSW.Rules.SharePointExtractor.Models
{
    public class SpRulesDataSet
    {
        
        public IList<Category> Categories { get; set; } = new List<Category>();
        
        public IList<RulePage> Rules { get; set; } = new List<RulePage>();
        
        
        public IList<Employee> Employees { get; set; } = new List<Employee>();

        
        public IList<ParentCategory> ParentCategories { get; set; } = new List<ParentCategory>();



        public Category CategoryByTitle(string title)
        {
            var tmpCat = new Category() { Title = title }; // create tmp category object to perform title->name conversion
            return Categories.FirstOrDefault(c => c.Name == tmpCat.Name);
        }

    }
}