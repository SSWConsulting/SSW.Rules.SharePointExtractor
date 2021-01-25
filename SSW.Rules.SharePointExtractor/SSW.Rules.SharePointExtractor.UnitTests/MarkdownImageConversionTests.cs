using FluentAssertions;
using FluentAssertions.Common;
using SSW.Rules.SharePointExtractor.Helpers;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class MarkdownImageConversionTests
    {
        [Fact]
        public void FigureImagesRemoveDl()
        {
            string dl = @"This will help to solidify the changes and alleviate confusion.
<dl class=""image""><br><br>::: ok<br>![Figure: Explaining the change that has been made using the prefix ""UPDATE:"". Using brackets is also an option](AppointmentWithComments.jpg)  <br>:::<br></dl>
### Related Rule";

            var converted = HtmlHelper.RemoveNode(dl, "dl", true);
            converted.Should().Be(@"This will help to solidify the changes and alleviate confusion.
<br><br>::: ok<br>![Figure: Explaining the change that has been made using the prefix ""UPDATE:"". Using brackets is also an option](AppointmentWithComments.jpg)  <br>:::<br>
### Related Rule");
        }

[Fact]
        public void ConvertDlImageFigure()
        {
            string figureHtml = @"<dl class=""image""><dt><img src = ""image.jpg""/></dt><dd>Figure: text</dd></dl>";
            string converted = HtmlHelper.ReplaceDlTagsWithImageFigures(figureHtml);
            converted.Should().Be(@"<dl class=""image""><br><br>::: ok  <br>![Figure: text](image.jpg)  <br>:::<br></dl>");
        }

        [Fact]
        public void RemoveDlTags()
        {
            string dlFigure = @"<dl class=""image""><br><br>::: good  <br>![Figure: Good example - a 5x scaled paper plane icon added to a Web Application](18-06-2014 2-33-45 PM.png)  <br>:::<br></dl>";
            string converted = HtmlHelper.RemoveNode(dlFigure, "dl", true);
            converted.Should().Be(@"<br><br>::: good  <br>![Figure: Good example - a 5x scaled paper plane icon added to a Web Application](18-06-2014 2-33-45 PM.png)  <br>:::<br>");
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
