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

            greyboxNodes = doc.DocumentNode.SelectNodes("//dt");
            result.UnionWith(FindClassName("greybox", greyboxNodes));

            return result;
        }

        public static string RemoveHtmlWithTagAndClassName(string html, string htmlTag, string className)
        {
            string result = html;
            var nodesFound = HtmlHelper.GetNodesWithTagAndClassName(html, htmlTag, className);

            if (nodesFound.Count > 0)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(result);

                foreach (var item in doc.DocumentNode.SelectNodes("//" + htmlTag))
                {
                    var className2 = item.Attributes["class"]?.Value;
                    if (className2?.IndexOf(className, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        item.Remove();
                    }                    
                }
                return doc.DocumentNode.OuterHtml;
            }
            return result;
        }

        public static string ReplaceHtmlWithFullTagAndAttribute(string html, string oldHtmlTag, string attr, string attrValue, string newHtmlTag)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndAttribute(html, oldHtmlTag, attr, attrValue);

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        "<" + newHtmlTag + ">" + node.InnerHtml.Trim(' ') + "</" + newHtmlTag + ">");
                }
            }

            return result;
        }

        public static string ReplaceHtmlWithTagAndClassName(string html, string oldHtmlTag, string oldClassName, string newHtmlTag)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndClassName(html, oldHtmlTag, oldClassName);

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        "<" + newHtmlTag + ">" + node.InnerHtml.Trim(' ') + "</" + newHtmlTag + ">");
                }
            }

            return result;
        }

        public static string ReplaceHtmlWithFencedBlock(string html, string oldHtmlTag, string oldClassName, string className)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndClassName(html, oldHtmlTag, oldClassName);

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        FencedBlocks.Create(node.InnerHtml.Trim(' '), className));
                }
            }
            return result;
        }

        public static string ReplaceHtmlWithCodeBlock(string html, string oldHtmlTag, string oldClassName, string type)
        {
            string result = html;
            var nodes = HtmlHelper.GetNodesWithTagAndClassName(html, oldHtmlTag, oldClassName);

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.OuterHtml))
                {
                    result = result.Replace(node.OuterHtml,
                        CodeBlocks.Create(node.InnerHtml.Trim(' '), type));
                }
            }
            return result;
        }

        public static HtmlNodeCollection GetNodesWithTagAndAttribute(string content, string htmlTag, string attr, string attrValue)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            if (doc.DocumentNode == null) return null;
            if (content == null) return null;

            var htmlNodes = doc.DocumentNode.SelectNodes("//" + htmlTag);

            var result = new HtmlNodeCollection(doc.DocumentNode);
            if (htmlNodes == null) return result;

            foreach (var node in htmlNodes)
            {
                var attribute = node.Attributes[attr]?.Value;
                if (attribute?.IndexOf(attrValue, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        public static HtmlNodeCollection GetNodesWithTagAndClassName(string content, string htmlTag, string className)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            if (doc.DocumentNode == null) return null;
            if (content == null) return null;

            var htmlNodes = doc.DocumentNode.SelectNodes("//" + htmlTag);
            var result = FindClassNameNodes(className, htmlNodes);

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

        private static HtmlNodeCollection FindClassNameNodes(string classname, HtmlNodeCollection nodes)
        {
            var doc = new HtmlDocument();
            var result = new HtmlNodeCollection(doc.DocumentNode);

            if (nodes == null) return result;

            foreach (var node in nodes)
            {
                var className = node.Attributes["class"]?.Value;
                if (className?.IndexOf(classname, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(node);
                }
            }
            return result;
        }
    }
}
