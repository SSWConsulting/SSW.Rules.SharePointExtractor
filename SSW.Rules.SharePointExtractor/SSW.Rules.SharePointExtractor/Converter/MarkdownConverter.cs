using HtmlAgilityPack;
using ReverseMarkdown;
using System;
using System.Collections.Generic;

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

        public static string Convert(string html)
        {
            html = PreMarkdownConversion.Process(html);

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

            return result.Trim();
        } 
    }
}
