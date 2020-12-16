using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class Greybox
    {
        public static string Process(string html)
        {
            string result = html;

            result = result.Replace("<font class=\"ms-rteCustom-GreyBox\" >", "<font class=\"ms-rteCustom-GreyBox\">");
            result = result.Replace("<font  class=\"ms-rteCustom-GreyBox\">", "<font class=\"ms-rteCustom-GreyBox\">");

            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "p", "ssw15-rteElement-GreyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "p", "ms-rteCustom-GreyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "font", "ms-rteCustom-GreyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "div", "scrum-GreyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "div", "greyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "div", "ms-rteCustom-GreyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "div", "ssw15-rteElement-GreyBox", "greybox");

            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "dt", "greybox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "p", "greyBox", "greybox");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "font", "greyBox", "greybox");


            var boxes = HtmlHelper.GetGreyboxes(result);
            foreach (var box in boxes)
            {
                if (!string.IsNullOrEmpty(box))
                    result = result.Replace(box, FencedBlocks.Create(box, "greybox"));
            }
            
            return result;
        }
    }
}
