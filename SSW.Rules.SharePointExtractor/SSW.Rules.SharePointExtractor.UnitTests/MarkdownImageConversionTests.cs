using FluentAssertions;
using FluentAssertions.Common;
using SSW.Rules.SharePointExtractor.Helpers;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class MarkdownImageConversionTests
    {
        [Fact]
        public void ConvertDlImageFigure()
        {
            string figureHtml = @"<b>test</b>
                <div></div>
                <dl class=""image""> 
                   <dt> 
                      <img src = ""ProjectManagementSummary_Small.jpg"" border=""0"" alt="""" style=""margin: 5px; width: 600px; height: 461px;""/>
                   </dt> 
                   <dd>Figure: Classic stories of Project Management</dd> 
                </dl>
                <h4>heading</h4>
                <p>text</p>";
            string converted = HtmlHelper.ReplaceDlTagsWithImageFigures(figureHtml);
            converted.Should().Be(@"<b>test</b>
                <div></div>
                ![Figure: Classic stories of Project Management](ProjectManagementSummary_Small.jpg)  
                <h4>heading</h4>
                <p>text</p>");
        }

        [Fact]
        public void ConvertImageWithoutFigureOrAltText()
        {
            string test = "![](image.png)";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(test);

            converted.Should().Be("![](image.png)");
        }

        [Fact]
        public void ConvertImageWithoutFigureWithAltText()
        {
            string test = "![image.png](image.png)";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![](image.png)");
        }

        [Fact]
        public void ConvertImageWithFigureWithoutAltText()
        {
            string test = "![](image.png) Figure: this is a caption";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageWithFigureWithAltText()
        {
            string test = "![image.png](image.png) Figure: this is a caption";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageWithFigureOnAnotherLine()
        {
            string test = "![](image.png)\r\nFigure: this is a caption";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageWithFigureOnAnotherLineBold()
        {
            string test = "![](image.png)\r\n**Figure: this is a caption**";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageGoodExample()
        {
            string test = "![](image.png)\r\n**Figure: Good example - this is a caption**";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("[[goodExample]]\r\n| ![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageBadExample()
        {
            string test = "![](image.png)\r\n**Figure: Bad example - this is a caption**";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("[[badExample]]\r\n| ![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageOkExample()
        {
            string test = "![](image.png)\r\n**Figure: Ok example - this is a caption**";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("[[okExample]]\r\n| ![this is a caption](image.png)");
        }

        [Fact]
        public void ConvertImageWithLink()
        {
            string test = "![](image.png)(www.test.com)";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![](image.png)(www.test.com)");
        }

        [Fact]
        public void ConvertImageWithLinkInFigure()
        {
            string test = "![](image.png) Figure: this is a caption with a URL www.test.com";
            string converted = MarkdownImages.RemoveAltIfNoFigure(test);
            converted = MarkdownImages.FixFigures(converted);

            converted.Should().Be("![this is a caption with a URL www.test.com](image.png)");
        }
    }
}
