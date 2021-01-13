using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class MarkdownImages
    {
        public static string RemoveAltIfFilename(string result)
        {
            //Find all Markdown images and remove alt text if the text is the same as the image filename
            MatchEvaluator removeAltEvaluator = new MatchEvaluator(RemoveAltIfFilenameEvaluator);
            result = Regex.Replace(result, @"\!\[[^\]]+\]\([^\)]+\)", removeAltEvaluator);
            return result;
        }

        public static string RemoveAltIfNoFigure(string result)
        {
            //remove alt text if no figure - See Tests for examples
            MatchEvaluator altEvaluator = new MatchEvaluator(RemoveAltEvaluator);
            result = Regex.Replace(result, @"\!\[[^\)]*(\]\(){1}[^\)]*\){1}(?!(\r*\n*\**\**Figure:))", altEvaluator, RegexOptions.Multiline);
            return result;
        }

        private static string FixImgFigureEvaluator(Match match)
        {
            string result;
            String[] separator= new string[] { };
            if (match.Value.ToLower().Contains("figure"))
            {
                separator = new string[] { "Figure :", "figure :", "Figure:", "figure:", "Figure -", "figure -", "Figure -", "figure -" };
            }
            else if (match.Value.ToLower().Contains("good example:"))
            {
                separator = new string[] { "Good example :", "Good Example :", "Good example:", "Good example -", "Good Example:", "Good Example -" };
            }
            else if (match.Value.ToLower().Contains("bad example:"))
            {
                separator = new string[] { "Bad example :", "Bad Example :", "Bad example:", "Bad example -", "Bad Example:", "Bad Example -" };
            }
            else if (match.Value.ToLower().Contains("ok example:"))
            {
                separator = new string[] { "Ok example:", "Ok Example:", "OK example:", "OK Example:", "Ok example -", "OK example -", "Ok Example -", "OK Example -" };
            }



            var elts = match.Value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            if (elts.Length > 1 && match.Value.StartsWith("![Figure"))
            {
                var temp = match.Value.Replace("Figure:", "").Replace("Figure -", "");

                result = temp.Substring(0, temp.LastIndexOf(elts[2].Trim()));
            }
            else
            {

                String[] separatorFile = { "](" };
                var imgInfo = elts[0].Split(separatorFile, StringSplitOptions.RemoveEmptyEntries);
                if (elts.Length > 1)
                    result = elts[0].Replace(imgInfo[0].Trim() + "]", "![" + elts[1].Trim() + "]");
                else
                    result = elts[0];

            }
            result = AddTag(result);
            return result.Trim();

        }
        public static string FixFigures(string result)
        {
            MatchEvaluator figureEvaluator = new MatchEvaluator(FixImgFigureEvaluator);
            result = Regex.Replace(result, @"\!\[[^\)]*\){1}(\r\n)*\s*(\*\*)*((Figure(\s*(:|-|-)))|((Good|Bad|Ok)( example)(\s*(:|-|-))))[^(\!\[)(\r\n)]*", figureEvaluator, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return result;
        }

        private static string AddTag(string result)
        {
            result = result.Replace("**", "").Replace("\r", "").Replace("\n", "");

            result = Regex.Replace(result, @"(\!\[Good example)\s*(-|–|:){1}\s*", "[[goodExample]]\r\n| ![", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(\!\[Bad example)\s*(-|–|:){1}\s*", "[[badExample]]\r\n| ![", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"(\!\[Ok example)\s*(-|–|:){1}\s*", "[[okExample]]\r\n| ![", RegexOptions.IgnoreCase);

            return result + "\n";
        }

        private static string RemoveAltEvaluator(Match match)
        {
            string result;
            String[] separatorFile = { "](" };
            var imgInfo = match.Value.Split(separatorFile, StringSplitOptions.RemoveEmptyEntries);
            if (imgInfo.Length > 1)
            {
                result = "![](" + imgInfo[1];
                if (imgInfo.Length == 3)
                {
                    result = result.Trim() + "](" + imgInfo[2];

                }
                return result;
            }
            else
            {
                return match.Value;
            }
        }

        private static string RemoveAltIfFilenameEvaluator(Match match)
        {
            string result;
            String[] separatorFile = { "](" };
            var imgInfo = match.Value.Split(separatorFile, StringSplitOptions.RemoveEmptyEntries);
            if (imgInfo.Length > 1)
            {
                var alt = imgInfo[0].Substring(2);
                var src = imgInfo[1].Remove(imgInfo[1].Length - 1);
                if (alt.Equals(src))
                {
                    result = "![](" + imgInfo[1];
                    return result;
                } else
                {
                    return match.Value;
                }
            }
            else
            {
                return match.Value;
            }
        }
    }
}
