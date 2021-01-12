using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string EscapeTagsInPre(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var htmlNodes = doc.DocumentNode.SelectNodes("//pre");
            if (htmlNodes != null) { 
            foreach(var node in htmlNodes)
            {
                node.InnerHtml = node.InnerHtml.Replace("<", "&lt;");
                node.InnerHtml = node.InnerHtml.Replace("<", "&gt;");
            }
            }
            return doc.DocumentNode.InnerHtml;
        }


        public static string ReplaceHtmlWithTag(string html, string oldHtmlTag)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            var nodes = HtmlHelper.GetNodesWithTag(doc, oldHtmlTag)?.OrderByDescending(n => n.Depth);
            while (nodes != null && nodes.Count() > 0)
            {
                var node = nodes.First();
                HtmlNode parent = null;
                var parentXpath = node.ParentNode.XPath;
                if (parentXpath != doc.DocumentNode.XPath)
                {
                    parent = doc.DocumentNode.SelectSingleNode(parentXpath);
                }
                else
                {
                    parent = doc.DocumentNode;
                }
                if (parent != null && !string.IsNullOrEmpty(node.OuterHtml))
                {
                    bool first = true;

                    HtmlNode firstChildNode = null;
                    foreach (var child in node.ChildNodes)
                    {
                        if (string.IsNullOrEmpty(child.OuterHtml.Trim()))
                            continue;
                        var newNode = HtmlNode.CreateNode(child.OuterHtml.Trim());
                        if (first)
                        {
                            parent.InsertAfter(newNode, node);
                            firstChildNode = newNode;
                            first = false;
                        }
                        else
                        {
                            parent.InsertAfter(newNode, firstChildNode);
                        }

                    }
                    parent.RemoveChild(node);
                }
                nodes = HtmlHelper.GetNodesWithTag(doc, oldHtmlTag)?.OrderByDescending(n => n.Depth);
            }

            return doc.DocumentNode.InnerHtml;
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

        public static string ReplaceDlTagsWithImageFigures(string html)
        {
            string result = html;
            
            if (html.Contains("<dl"))
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(result);

                foreach (var item in doc.DocumentNode.SelectNodes("//dl"))
                {
                    var className = item.Attributes["class"]?.Value;
                    var figureType = "";
                    var imgSrc = "";
                    var figCaption = "";

                    if (className?.IndexOf("badImage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        figureType = "bad";
                    } else if (className?.IndexOf("goodImage", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        figureType = "good";
                    } else if(className?.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        figureType = "ok";
                    }

                    if (figureType != "")
                    {
                        try
                        {
                            //Item - get dt node (Image source)
                            var itemDt = item.SelectNodes("//dt");
                            if (itemDt != null)
                            {
                                var itemImg = itemDt.First().SelectNodes("//img");
                                if(itemImg != null)
                                {
                                    imgSrc = itemImg.First().GetAttributeValue("src", "");
                                }
                            }

                            //Item - get dd node (Figcaption)
                            var itemDd = item.SelectNodes("//dt/dd");
                            if (itemDd != null)
                            {
                                //TODO: Check that this is the correct one
                                figCaption = itemDd.First().InnerText.Trim();
                            } else
                            {
                                //Check the dt node for the Figure Text
                                figCaption = item.InnerText.Trim();
                            }

                            if(imgSrc != "")
                            {
                                if(figureType == "ok")
                                {
                                    if (figCaption.ToLower().Contains("figure: good example"))
                                    {
                                        figureType = "good";

                                    } else if(figCaption.ToLower().Contains("figure: bad example"))
                                    {
                                        figureType = "bad";
                                    } else
                                    {
                                        figureType = "ok";
                                    }
                                }

                                if(imgSrc == "BeforeCoding.jpg")
                                {
                                    var t = "";
                                }

                                var imageFigure = ImageFigure.Create(figureType, figCaption, imgSrc);
                                imageFigure = imageFigure.Replace("<br>", "{brHTML}");

                                var newNode = HtmlNode.CreateNode(imageFigure);
                                item.ParentNode.InsertBefore(newNode, item);
                                item.Remove();
                            }
                        }
                        catch (Exception ex)
                        {
                            //TODO: Log Error
                            var t = "error";
                        }
                    }
                }
                return doc.DocumentNode.OuterHtml.Replace("{brHTML}","<br>");
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
        public static List<HtmlNode> GetNodesWithTag(HtmlDocument doc, string htmlTag)
        {
            var htmlNodes = doc.DocumentNode.SelectNodes("//" + htmlTag);

            if (htmlNodes == null)
                return null;

            //remove if parent <pre>
            // remove if outerhtml is empty
            var filterNode = new List<HtmlNode>();
            foreach(var node in htmlNodes)
            {
                if (string.IsNullOrEmpty(node.OuterHtml))
                    continue; 
                if(node.XPath.Contains("/pre"))
                    continue;
                filterNode.Add(node);
            }
            return filterNode;
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
