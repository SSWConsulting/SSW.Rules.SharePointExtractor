using SSW.Rules.SharePointExtractor.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Converter
{
    class PostMarkdownConversion
    {

        public static string Process(string markdown)
        {
            string result = markdown;

            //Add line breaks to Markdown text
            result = CleanFigureMarkdown(result);
            result = ReplaceHtmlLineBreaks(result);
            result = FixMultilineFigures(result);

            //Fix images and figures
            result = RemoveExternalLinkImages(result);
            result = MarkdownImages.RemoveAltIfNoFigure(result);
            result = MarkdownImages.FixFigures(result);

            //Tidy up Markdown
            result = result.Replace("<this is=\"\" as=\"\" per=\"\" the=\"\" rule=\"\"></this>", "< This is as per the rule ");
            result = result.Replace("<this email=\"\" was=\"\" sent=\"\" as=\"\" per=\"\"></this>", "< This email was sent as per ");

            //This endintro marker is for showing the rule blurb on the category page
            result = result.Replace("<excerpt class='endintro'></excerpt>", "<!--endintro-->");

            return result;
        }

        private static string CleanFigureMarkdown(string result)
        {
            result = result
                .Replace(@"[!\[", "[![")
                .Replace(@"\](", "](")
                .Replace("![", "\n![");
            return result;
        }

        private static string ReplaceHtmlLineBreaks(string result)
        {
            result = result
                .Replace("]]<br>|", "]]\r\n|")
                .Replace("]<br>", "]")
                .Replace("<br>[", "[");
            return result;
        }
        
        private static string FixMultilineFigures(string result)
        {
            result = Regex.Replace(result, @"(\n\|\s*\n*)", "\n| ", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return result;
        }

        private static string RemoveExternalLinkImages(string result)
        {
            //We use CSS to add these .rule-content a[href*="//"]:not([href*="rules.ssw.com.au"]):after {
            result = result.Replace("![](external.gif \"You are now leaving SSW\")", "")
                    .Replace("![](external.gif \"You are now leaving SSW\")", "")
                    .Replace("![](../../assets/external.gif \"You are now leaving SSW\")", "")
                    .Replace("![Leave site](../../assets/LeaveSite.gif)", "")
                    .Replace("![leave site](../../assets/LeaveSite.gif)", "")
                    .Replace("![You are about to leave the SSW site](../../assets/LeaveSite.gif)", "")
                    .Replace("![Leave Site](../../assets/LeaveSite.gif)", "");
            return result;
        }

        
    }
}
