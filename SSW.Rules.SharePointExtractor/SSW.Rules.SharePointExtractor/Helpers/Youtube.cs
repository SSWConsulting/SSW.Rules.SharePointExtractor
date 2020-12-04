using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class Youtube
    {

        public static string convertYoutubeVideos(string html)
        {
            MatchEvaluator youtubeEvaluator = new MatchEvaluator(YoutubeEvaluator);
            string result = Regex.Replace(html, @"<iframe.*(youtu.?be).*</iframe>", youtubeEvaluator, RegexOptions.Multiline);
           
            return result;
        }

        private static string YoutubeEvaluator(Match match)
        {
            string result = Regex.Match(match.Value, "src\\s?=\\s?\"(.*?)\"").Value;
            if (!string.IsNullOrEmpty(result))
                result = $"<br/>`youtube: {result.Split('"')[1]}`<br/>";
            return result;
        }
    }
}
