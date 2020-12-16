using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class HtmlSpan
    {
        public static string Process(string html)
        {
            string result = html;

            result = ReplaceHighlighedSpans(result);
            result = ReplaceFigureSpans(result);
            return result;
        }

        public static string ReplaceHighlighedSpans(string html)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndClassName(html, "span", "ssw15-rteStyle-Highlight");
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        "<mark>" + node.InnerHtml.Trim(' ') + "</mark>");
                }
            }
            return result;
        }

        public static string ReplaceFigureSpans(string html)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndClassName(html, "span", "ms-rteCustom-FigureNormal");
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        "<strong>" + node.InnerHtml.Trim(' ') + "</strong>");
                }
            }
            return result;
        }
    }
}
