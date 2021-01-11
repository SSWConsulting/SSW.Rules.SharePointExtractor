using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class EncodedHtmlTags
    {
        public static string Encode(string content)
        {
            content = content.Replace("&lt;", "{ltHTMLChar}");
            content = content.Replace("&gt;", "{gtHTMLChar}");
            return content;
        }

        public static string Decode(string content)
        {
            content = content.Replace("{ltHTMLChar}","&lt;");
            content = content.Replace("{gtHTMLChar}", "&gt;");
            return content;
        }
    }
}
