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

        public static string ConvertTagsInPre(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var htmlNodes = doc.DocumentNode.SelectNodes("//pre");
            if (htmlNodes != null) { 
            foreach(var node in htmlNodes)
            {
                node.InnerHtml = node.InnerHtml.Replace("<br>", Environment.NewLine);
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

        public static string RemoveNode(string html, string htmlTag, bool keepChildren)
        {
            string result = html;

            var doc = new HtmlDocument();
            doc.LoadHtml(result);

            var nodeList = doc.DocumentNode.SelectNodes(htmlTag);

            if(nodeList != null)
            {
                foreach (var node in nodeList)
                {
                    doc.DocumentNode.RemoveChild(node, keepChildren);
                }

                return doc.DocumentNode.OuterHtml;
            }
            return html;
        }

        public static string ReplaceDlTagsWithImageFigures(string html)
        {
            string result = html;
            
            //Do a quick search for dl tags
            if (html.Contains("<dl"))
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(result);

                foreach (var dlNode in doc.DocumentNode.SelectNodes("//dl"))
                {
                    //If we can't find any img nodes, then ignore the <dl>
                    var imageNodes = dlNode.SelectNodes("//dl//dt//img");
                    if(imageNodes == null)
                    {
                        continue;
                    }

                    var dlClassName = dlNode.Attributes["class"]?.Value;
                    var figureType = "";
                    var imgSrc = "";
                    var figCaption = "";

                    if (dlClassName?.IndexOf("badImage", StringComparison.OrdinalIgnoreCase) >= 0) {
                        figureType = "bad";
                    }
                    else if (dlClassName?.IndexOf("goodImage", StringComparison.OrdinalIgnoreCase) >= 0) {
                        figureType = "good";
                    }
                    else if (dlClassName?.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        figureType = "ok";
                    }

                    if (figureType != "")
                    {
                        var xpathsToRemove = new List<string>();
                        var nodesToAdd = new Dictionary<HtmlNode, HtmlNode>(); 
                        //Go through the <dl> node by node
                        foreach (var node in dlNode.ChildNodes)
                        {
                            if (node.Name.Equals("dt"))
                            {
                                //Check if this is an image node, then process it
                                //Get the img node (Image source)
                                var imgNode = node.SelectSingleNode("img");
                                if (imgNode != null)
                                {
                                    imgSrc = imgNode.GetAttributeValue("src", "");

                                    //Get the related dd node (Figcaption) (next sibling)
                                    HtmlNode sibling = node.NextSibling;
                                    HtmlNode ddNode = null;
                                    bool nextImageFound = false;
                                    while (sibling != null && ddNode == null && nextImageFound == false)
                                    {
                                        if (sibling.NodeType == HtmlNodeType.Element)
                                        {
                                            if (sibling.Name.Equals("dt"))
                                            {
                                                //We found another image
                                                nextImageFound = true;
                                            }

                                            if (sibling.Name.Equals("dd"))
                                            {
                                                ddNode = sibling;
                                            }
                                        }
                                        sibling = sibling.NextSibling;
                                    }

                                    if (ddNode != null)
                                    {
                                        figCaption = ddNode.InnerText.Trim();
                                    }
                                    else
                                    {
                                        //Check the dt node for the Figure Text
                                        figCaption = imgNode.InnerText.Trim();
                                    }

                                    if (imgSrc != "")
                                    {
                                        if (figureType == "ok")
                                        {
                                            if (figCaption.ToLower().Contains("figure: good example"))
                                            {
                                                figureType = "good";

                                            }
                                            else if (figCaption.ToLower().Contains("figure: bad example"))
                                            {
                                                figureType = "bad";
                                            }
                                            else
                                            {
                                                figureType = "";
                                            }
                                        }

                                        var imageFigure = ImageFigure.Create(figureType, figCaption, imgSrc);
                                        imageFigure = imageFigure.Replace("<br>", "{brHTML}");

                                        var newNode = HtmlNode.CreateNode(imageFigure);
                                        nodesToAdd.Add(node, newNode);

                                        xpathsToRemove.Add(node.XPath);

                                        if(ddNode != null)
                                        {
                                            xpathsToRemove.Add(ddNode.XPath);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //If it's anything else, then ignore it
                            }
                        }

                        foreach(var nodePair in nodesToAdd)
                        {
                            var node = nodePair.Key;
                            node.ParentNode.InsertAfter(nodePair.Value, node);
                        }

                        foreach (string xpath in xpathsToRemove)
                        {
                            var node = doc.DocumentNode.SelectSingleNode(xpath);

                            if (node != null)
                            {
                                node.Remove();
                            }
                        }
                    }
                }
                return doc.DocumentNode.OuterHtml.Replace("{brHTML}","<br>");
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
