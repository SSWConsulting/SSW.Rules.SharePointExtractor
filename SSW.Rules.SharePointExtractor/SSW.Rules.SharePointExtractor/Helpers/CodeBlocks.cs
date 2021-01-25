using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class CodeBlocks
    {
        public static string Create(string content, string type)
        {
            var fencedBlock = "<br><pre>";
            fencedBlock += content.Replace("<br>", Environment.NewLine);
            fencedBlock += "<br></pre><br>";
            return fencedBlock;
        }
    }
}
