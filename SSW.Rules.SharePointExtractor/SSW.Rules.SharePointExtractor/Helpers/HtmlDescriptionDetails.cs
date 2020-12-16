using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class HtmlDescriptionDetails
    {
        public static string Process(string html)
        {
            string result = html;

            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "dd", "ssw15-rteElement-FigureNormal", "strong");
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "dd", "ms-rteCustom-FigureNormal", "strong");

            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "dd", "ssw15-rteElement-FigureBad", "bad");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "dd", "ms-rteCustom-FigureBad", "bad");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "dd", "ssw15-rteElement-FigureGood", "good");
            
            //Clean HTML
            /*
            result = result.Replace("<dd class=\"ssw15-rteElement-FigureNormal\">", "");
            result = result.Replace("<dd class=\"ssw -rteStyle-FigureNormal\">", "");
            result = result.Replace("<dd class='ssw15-rteElement-FigureBad'>", "");
            result = result.Replace("<dd class=\"ssw15-rteElement-FigureBad\">", "");
            result = result.Replace("<dd class='ssw15-rteElement-FigureGood'>", "");
            result = result.Replace("<dd class=\"ssw15-rteElement-FigureGood\">", "");
            result = result.Replace("<dd class=\"ms-rteCustom-FigureBad\">", "");
            result = result.Replace("<dd class=\"ms-rteCustom-FigureGood\">","");
            result = result.Replace("<dd style=\"border:none;line-height:16px;\">","");

            result = result.Replace("<dd>", "");
            */

            return result;
        }
    }
}
