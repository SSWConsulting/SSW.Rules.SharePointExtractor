using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Models
{
    public interface IHasContentHistory
    { 
        IList<ContentVersion> Versions { get; set; }
    }
}
