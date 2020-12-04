using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.SpImporter
{
    public interface ISpImporter
    {
        SpRulesDataSet Import();
    }
}