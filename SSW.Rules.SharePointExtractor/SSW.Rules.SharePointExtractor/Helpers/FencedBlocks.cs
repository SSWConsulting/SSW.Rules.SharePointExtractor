using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class FencedBlocks
    {
        public static string Create(string content, string type)
        {
            var fencedBlock = "<br>::: " + type + "<br>";
            fencedBlock += content.Trim() + "  <br>";
            fencedBlock += ":::<br>";
            return fencedBlock;
        }
    }
}
