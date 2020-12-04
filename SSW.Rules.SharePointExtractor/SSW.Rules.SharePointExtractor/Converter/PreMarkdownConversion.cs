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

            //Remove leading and trailing whitespace
            result = TrimWhitespaceAroundBoldText(result);          

            //Convert embedded YouTube videos to markdown
            result = Youtube.convertYoutubeVideos(result);
            result = Greybox.Process(result);

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
