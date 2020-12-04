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

            var boxes = HtmlHelper.GetGreyboxes(html);
            foreach (var box in boxes)
            {
                if (!string.IsNullOrEmpty(box))
                    result = result.Replace(box, $"<br>[greyBox] {box} [/greyBox]");
            }
            return result;
        }

        public static string ProcessMarkdown(string md)
        {
            string result = md;

            result = Regex.Replace(result, @"(\[greyBox\]){1}(\s*.+?\s*)+?(\[\/greyBox\]){1}", greyBoxEvaluator, RegexOptions.Multiline);

            return result;
        }

        private static string greyBoxEvaluator(Match match)
        {
            string result= match.Value.Replace("[greyBox]", "[[greyBox]]")
                .Replace("\r\n[/greyBox]", "")
                .Replace("\r\n", "\r\n| ");


            return result;
        }


        public static string FixFigures(string result)
        {
            MatchEvaluator figureEvaluator = new MatchEvaluator(FixFigureEvaluator);
            try {
                TimeSpan timeout = new TimeSpan(0, 0, 1);
                result = Regex.Replace(result, @"(\[\[greyBox\]\]){1}((\r*\n*\|)+.*)+?\n\s*(((Figure(\s*(:|-|-)))|((Good|Bad|Ok)( example)(\s*(:|-|-)))){1}[^(\r\n)]*)", figureEvaluator, RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, timeout);
            }catch(RegexMatchTimeoutException e)
            {
                return result;
            }
                return result;
        }

        private static string FixFigureEvaluator(Match match)
        {
            string result;
            String[] separator = new string[] { };
            string tag = "greyBox";
            
            if (match.Value.ToLower().Contains("good example"))
            {
                separator = new string[] { "Good example :", "Good Example :", "Good example:", "Good example -", "Good Example:", "Good Example -" };
                tag = "goodExample";
            }
            else if (match.Value.ToLower().Contains("bad example"))
            {
                separator = new string[] { "Bad example :", "Bad Example :", "Bad example:", "Bad example -", "Bad Example:", "Bad Example -","Bad example - ", "Bad Example - " };
                tag = "badExample";
            }
            else if (match.Value.ToLower().Contains("ok example"))
            {
                tag = "okExample"; 
                separator = new string[] { "Ok example:", "Ok Example:", "OK example:", "OK Example:", "Ok example -", "OK example -", "Ok Example -", "OK Example -" };
            }
            else if (match.Value.ToLower().Contains("figure"))
            {
                separator = new string[] { "Figure :", "figure :", "Figure:", "figure:", "Figure -", "figure -", "Figure -", "figure -" };
            }


            var elts = match.Value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (elts.Length == 1)
            {
                result = elts[0].Replace("[[greyBox]]", $"[[{tag}]]");
            }
            else { 
                result = elts[0].Replace("[[greyBox]]", $"[[{tag} | {elts[1].Trim()}]]");
            }
            if (result.EndsWith("Figure: "))
            {
                result = result.Substring(0, result.LastIndexOf("Figure:"));
            }

            //result = AddTag(result);
            return result.Trim();

        }
    }
}
