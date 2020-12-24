using SSW.Rules.SharePointExtractor.Helpers;
using System.Text.RegularExpressions;

namespace SSW.Rules.SharePointExtractor.Converter
{
    class PreMarkdownConversion
    {
        public static string Process(string html)
        {
            //Replace space characters
            string result = html.Replace("\u200B", "").Replace("&nbsp;", " ");
            result = result.Replace("size=\"+0\"","");
            result = result.Replace("<s>", "~~").Replace("</s>", "~~");

            result = result.Replace("<title>", "&lt;title&gt;");
            result = result.Replace("<option>", "&lt;option&gt;");
            result = result.Replace("</option>", "&lt;/option&gt;");
            result = result.Replace("<label>", "&lt;label&gt;");
            result = result.Replace("</label>", "&lt;/label&gt;");
            result = result.Replace("<fieldset>", "&lt;fieldset&gt;");
            result = result.Replace("</fieldset>", "&lt;/fieldset&gt;");
            result = result.Replace("<CollectionUrl>", "&lt;CollectionUrl&gt;");
            result = result.Replace("<yourname>", "&lt;yourname&gt;");
            result = result.Replace("<yourdomain>", "&lt;yourdomain&gt;");
            result = result.Replace("<Agent’s name>", "&lt;Agent’s name&gt;");
           
            //Convert Span Highlights
            result = HtmlSpan.Process(result);

            //Convert embedded YouTube videos to markdown
            result = Youtube.convertYoutubeVideos(result);
            result = Greybox.Process(result);

            //Remove unhandled tags
            //result = HtmlDescriptionList.Process(result);
            result = HtmlDescriptionDetails.Process(result);

            result = HtmlHelper.ReplaceHtmlWithTag(result, "dl");
            result = HtmlHelper.ReplaceHtmlWithTag(result, "dt");
            result = HtmlHelper.ReplaceHtmlWithTag(result, "dd");

            result = HtmlHelper.EscapeTagsInPre(result);

            result = HtmlFont.Process(result);

            //Remove leading and trailing whitespace
            result = TrimWhitespaceAroundBoldText(result);

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
