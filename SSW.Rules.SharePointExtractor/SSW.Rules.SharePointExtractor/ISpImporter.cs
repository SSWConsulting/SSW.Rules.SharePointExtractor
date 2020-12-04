using System.Threading.Tasks;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor
{
    public interface ISpImporter
    {
        Task<SpRulesDataSet> Import();
    }
}