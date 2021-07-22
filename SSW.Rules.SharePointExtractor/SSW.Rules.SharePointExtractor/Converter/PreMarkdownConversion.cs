using SSW.Rules.SharePointExtractor.Helpers;
using System;
using Serilog;
using System.Text.RegularExpressions;

namespace SSW.Rules.SharePointExtractor.Converter
{
    class PreMarkdownConversion
    {
        public static string Process(string html)
        {
            // Check for web parts - these cannot be migrated
            HtmlHelper.FindWebParts(html);

            // TODO: Check for references of /Documents folder - these files are not migrated

            //Replace space characters
            string result = html.Replace("\u200B", "").Replace("&nbsp;", " ");
            result = result.Replace("size=\"+0\"","");
            result = result.Replace("<s>", "~~").Replace("</s>", "~~");
           
            //Convert Span Highlights
            result = HtmlSpan.Process(result);

            //Convert embedded YouTube videos to markdown
            result = Youtube.convertYoutubeVideos(result);
            result = Greybox.Process(result);

            //Remove unhandled tags
            result = HtmlDescriptionList.Process(result);
            result = HtmlDescriptionDetails.Process(result);

            //Remove nodes, but keep the child nodes
            result = HtmlHelper.RemoveNode(result, "dl", true);
            result = HtmlHelper.RemoveNode(result, "dt", true);
            result = HtmlHelper.RemoveNode(result, "dd", true);
            
            result = HtmlHelper.ConvertTagsInPre(result);

            result = HtmlFont.Process(result);

            //Remove leading and trailing whitespace
            result = TrimWhitespaceAroundBoldText(result);

            result = HtmlHelper.RemoveNode(result, "dl", true);

            return result;
        }

        private static string TrimWhitespaceAroundBoldText(string result)
        {
            result = Regex.Replace(result, @"(<b>)\s*", " <b>");
            result = Regex.Replace(result, @"(<strong>)\s*", " <strong>");
            result = Regex.Replace(result, @"\s*(</b>\s*)", "</b> ");
            result = Regex.Replace(result, @"\s*(</strong>\s*)", "</strong> ");
            return result;
        }
    }
}
