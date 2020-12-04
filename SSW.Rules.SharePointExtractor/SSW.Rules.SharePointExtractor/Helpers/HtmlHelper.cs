using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class HtmlHelper
    {
        public static HashSet<string> GetImageUrls(string content)
        {
            var result = new HashSet<string>();
            if (content == null) return result;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            if (doc.DocumentNode == null) return result;

            var imageNodes = doc.DocumentNode.SelectNodes("//img");
            if (imageNodes == null) return result;

            foreach (var imgNode in imageNodes)
            {
                var src = imgNode.Attributes["src"]?.Value;
                if (src == null) continue;
                result.Add(WebUtility.HtmlDecode(src));
            }

            return result;
        }

        public static HashSet<string> GetGreyboxes(string content)
        {
            var result = new HashSet<string>();
            if (content == null) return result;

            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            if (doc.DocumentNode == null) return result;

            var greyboxNodes = doc.DocumentNode.SelectNodes("//div");
            result = FindClassName("greybox", greyboxNodes);

            greyboxNodes = doc.DocumentNode.SelectNodes("//p");
            result.UnionWith(FindClassName("greybox", greyboxNodes));

            greyboxNodes = doc.DocumentNode.SelectNodes("//font");
            result.UnionWith(FindClassName("greybox", greyboxNodes));

            return result;
        }

        private static HashSet<string> FindClassName(string classname, HtmlNodeCollection nodes)
        {
            var result = new HashSet<string>();
            if (nodes == null) return result;

            foreach (var node in nodes)
            {
                var className = node.Attributes["class"]?.Value;
                if (className?.IndexOf(classname, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(node.InnerHtml);
                }
            }
            return result;
        }
    }
}
