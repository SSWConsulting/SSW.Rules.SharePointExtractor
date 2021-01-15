using FluentAssertions;
using FluentAssertions.Common;
using SSW.Rules.SharePointExtractor.Helpers;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class MarkdownFigureConversionTests
    {
        [Fact]
        public void CreateGreyBox()
        {
            string content = "this is a sample greybox block";
            string converted = FencedBlocks.Create(content, "greybox");
            converted.Should().Be("<br>::: greybox<br>" + content+ "  <br>:::<br>");
        }

        [Fact]
        public void CreateChinaBox()
        {
            string content = "this is a sample china block";
            string converted = FencedBlocks.Create(content, "china");
            converted.Should().Be("<br>::: china<br>" + content + "  <br>:::<br>");
        }

        [Fact]
        public void CreateInfoBox()
        {
            string content = "this is a sample info block";
            string converted = FencedBlocks.Create(content, "info");
            converted.Should().Be("<br>::: info<br>" + content + "  <br>:::<br>");
        }

        [Fact]
        public void CreateWarningBox()
        {
            string content = "this is a sample warning block";
            string converted = FencedBlocks.Create(content, "warning");
            converted.Should().Be("<br>::: warning<br>" + content + "  <br>:::<br>");
        }

        [Fact]
        public void CreateDevBox()
        {
            string content = "this is a sample dev block";
            string converted = FencedBlocks.Create(content, "dev");
            converted.Should().Be("<br>::: dev<br>" + content + "  <br>:::<br>");
        }

        [Fact]
        public void CreateTaskBox()
        {
            string content = "this is a sample task block";
            string converted = FencedBlocks.Create(content, "task");
            converted.Should().Be("<br>::: task<br>" + content + "  <br>:::<br>");
        }

        [Fact]
        public void CreateEmailTemplate()
        {
            string content = "this is a sample email block";
            string converted = FencedBlocks.Create(content, "email");
            converted.Should().Be("<br>::: email<br>" + content + "  <br>:::<br>");
        }
    }
}
