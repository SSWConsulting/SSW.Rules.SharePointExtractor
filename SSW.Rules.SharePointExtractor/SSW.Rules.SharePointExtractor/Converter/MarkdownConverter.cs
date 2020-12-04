using ReverseMarkdown;

namespace SSW.Rules.SharePointExtractor.Converter
{
    public class MarkdownConverter
    {

        private static Config config = new ReverseMarkdown.Config
        {
            // Include the unknown tag completely in the result (default as well)
            UnknownTags = Config.UnknownTagsOption.Bypass,
            // generate GitHub flavoured markdown, supported for BR, PRE and table tags
            GithubFlavored = true,
            // remove markdown output for links where appropriate
            SmartHrefHandling = true
        };

        private static ReverseMarkdown.Converter converter = new ReverseMarkdown.Converter(config);

        

        public static string Convert(string html)
        {        
            html = PreMarkdownConversion.Process(html);

            string result = converter.Convert(html);

            result = PostMarkdownConversion.Process(result);

            if (string.IsNullOrEmpty(result))
            {
                return " ";
            }

            return result.Trim();
        } 
    }
}
