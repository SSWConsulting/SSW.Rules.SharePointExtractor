using HtmlAgilityPack;
using ReverseMarkdown;
using SSW.Rules.SharePointExtractor.MdWriter;
using SSW.Rules.SharePointExtractor.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SSW.Rules.SharePointExtractor.Converter
{
    public class MarkdownConverter
    {

        private static Config config = new ReverseMarkdown.Config
        {
            // Include the unknown tag completely in the result (default as well)
            UnknownTags = Config.UnknownTagsOption.PassThrough,
            // generate GitHub flavoured markdown, supported for BR, PRE and table tags
            GithubFlavored = true,
            ListBulletChar = '*',
            // remove markdown output for links where appropriate
            SmartHrefHandling = true
        };

        private static ReverseMarkdown.Converter converter = new ReverseMarkdown.Converter(config);

        public static string Convert(RuleHtmlPage ruleHtmlPage)
        {
            string html = PreMarkdownConversion.Process(ruleHtmlPage.Html);

            var ignoreHtmlTags = new HashSet<string> { "excerpt", "mark","sup", "dd", "dl", "dt", "font", "char", "type", "crmwebsiteroot" };

            string result = "";
            try
            {
                result = converter.Convert(html);
            } catch(Exception e)
            {
                var error = e;
                if(e.Message.Contains("Unknown tag: "))
                {
                    var tag = e.Message.Replace("Unknown tag: ", "");
                    if(ignoreHtmlTags.Contains(tag))
                    {

                    } else
                    {
                        result = html;
                    }
                }
            }
             

            result = PostMarkdownConversion.Process(result);

            if (string.IsNullOrEmpty(result))
            {
                return " ";
            }

            //Check for any URL Redirects
            if (Regex.IsMatch(result, @"(""(\/_layouts\/15\/FIXUPREDIRECT.ASPX)[^""]*"")"))
            {
                LogMarkdownConversionIssue(ruleHtmlPage, "URL Links", "Unresolved relative SharePoint URL");
            }

            return result.Trim();
        }

        public static void LogMarkdownConversionIssue(RuleHtmlPage ruleHtmlPage, string issue, string details)
        {
            string fileName = "MarkDownConversionIssues.csv";
            if (!System.IO.File.Exists(fileName))
            {
                System.IO.File.WriteAllText(fileName, "URL,RuleName,Issue,Details" + Environment.NewLine);
            }

            System.IO.File.AppendAllText(fileName, ruleHtmlPage.Uri + "," + ruleHtmlPage.Title + "," + issue + "," + details + Environment.NewLine);
        }
    }
}
