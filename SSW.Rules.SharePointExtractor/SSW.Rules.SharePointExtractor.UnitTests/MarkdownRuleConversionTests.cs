﻿using FluentAssertions;
using FluentAssertions.Common;
using SSW.Rules.SharePointExtractor.Converter;
using SSW.Rules.SharePointExtractor.Helpers;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class MarkdownRuleConversionTests
    {
        [Fact]
        public void ConvertRuleToMarkdown() //https://rules.ssw.com.au/use-the-right-html-figure-caption
        {
            string content = "<p class=\"ssw15-rteElement-CodeArea\">&lt;div&gt;<br>&#160;&#160;&lt;img alt=&quot;&quot;/&gt;<br>&#160; &lt;p&gt;Figure&#58; Caption&lt;/p&gt;<br>&lt;/div&gt; </p><dd class=\"ssw15-rteElement-FigureBad\">Figure&#58; Bad Example​ </dd><p>Instead, you should use \r\n   <b>&lt;figure&gt;</b> and \r\n   <b>&lt;figcaption&gt; </b>as per&#160;<a href=\"https&#58;//www.w3schools.com/TAGS/tag_figcaption.asp\">https&#58;//www.w3schools.com/TAGS/tag_figcaption.asp​</a>.&#160;This structure gives semantic&#160;meaning&#160;to&#160;the image and&#160;figure&#58;<br></p><p class=\"ssw15-rteElement-CodeArea\">&lt;figure&gt;<br>&#160;&#160;&lt;img&#160;src=&quot;image.jpg&quot;&#160;alt=&quot;Image&quot; /&gt;<br>&#160;&#160;&lt;figcaption&gt;Figure&#58; Caption&lt;/figcaption&gt;<br>&lt;/figure&gt; </p><dd class=\"ssw15-rteElement-FigureGood\">Figure&#58; Good Example​​​​​​<br></dd><h3 class=\"ssw15-rteElement-H3\">​​The old way​<br></h3><p>For some internal sites, we still use the old way to place images&#58; Using&#160;<b>&lt;dl&gt;</b>,&#160;<b>&lt;dt&gt;</b> (which is the item in the list – in our case an image), and \r\n   <b>&lt;dd&gt;</b>for a caption. \r\n   <br></p><p class=\"ssw15-rteElement-CodeArea\">&lt;dl class=&quot;image&quot;&gt; OR &lt;dl class=&quot;badImage&quot;&gt; OR &lt;dl class=&quot;goodImage&quot;&gt; <br>&#160; &lt;dt&gt;&lt;img src=&quot;image.jpg&quot;​ alt=&quot;Image&quot;/&gt;&lt;/dt&gt;<br>&#160; &lt;dd&gt;Figure&#58; Caption&lt;/dd&gt; <br>&lt;/dl&gt;<br></p><dd class=\"ssw15-rteElement-FigureNormal\"> \r\n​\r\n      \r\n      Figure&#58; Good Example​<br></dd><div>\r\n<p><b>​Note&#58;</b>&#160;&lt;dl&gt; stands for &quot;<b>definition list</b>&quot;; &lt;dt&gt; for &quot;<b>definition term</b>&quot;; and &lt;dd&gt; for &quot;<b>definition description</b>&quot;.<br></p><h3 class=\"ssw15-rteElement-H3\">​Relate Rule<br></h3><ul><li> \r\n      <a href=\"/_layouts/15/FIXUPREDIRECT.ASPX?WebId=3dfc0e07-e23a-4cbb-aac2-e778b71166a2&amp;TermSetId=07da3ddf-0924-4cd2-a6d4-a4809ae20160&amp;TermId=810b7dab-f94c-4495-bf88-bb80c3bc9776\">Figures - Do you add useful and concise figure text?​​​</a><br></li></ul></div>";

            string converted = MarkdownConverter.Convert(content);
            converted.Should().Be(@"");
        }

    }
}
