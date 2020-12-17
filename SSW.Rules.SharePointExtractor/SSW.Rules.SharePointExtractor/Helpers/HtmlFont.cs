using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class HtmlFont
    {
        public static string Process(string html)
        {
            string result = html;
 
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "g", "gr_ gr_12 gr-alert gr_gramm gr_disable_anim_appear undefined Punctuation multiReplace", "p");
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "g", "gr_ gr_26 gr-alert gr_gramm gr_inline_cards gr_run_anim Grammar multiReplace", "p");
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "g", "gr_ gr_42 gr-alert gr_gramm gr_inline_cards gr_run_anim Punctuation only-ins replaceWithoutSep", "p");
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "g", "gr_ gr_28 gr-alert gr_gramm gr_inline_cards gr_run_anim Style multiReplace", "p");
            result = HtmlHelper.ReplaceHtmlWithTagAndClassName(result, "font", "ms-rteCustom-FigureNormal", "strong");

            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "font", "ms-rteCustom-FigureGood", "good");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "font", "ms-rteCustom-FigureBad", "bad");
            result = HtmlHelper.ReplaceHtmlWithFencedBlock(result, "font", "ms-rteCustom-YellowBorderBox", "yellowBox");

            result = HtmlHelper.ReplaceHtmlWithFullTagAndAttribute(result, "font", "style", "background-color&#58;#ffff00;", "mark");

            result = HtmlHelper.RemoveHtmlWithTagAndClassName(result, "p", "ms-rteCustom-SSW-Only");
            result = HtmlHelper.RemoveHtmlWithTagAndClassName(result, "font", "ms-rteCustom-SSW-Only");
            result = HtmlHelper.RemoveHtmlWithTagAndClassName(result, "div", "ms-rteCustom-SSW-Only");
            result = HtmlHelper.RemoveHtmlWithTagAndClassName(result, "p", "ssw15-rteElement-SSW-Only");
            result = HtmlHelper.RemoveHtmlWithTagAndClassName(result, "div", "ssw15-rteElement-ContentBlock-SSW-Only");

            //Clean HTML
            /*
            result = result.Replace("<font class=\"ms-rteCustom-SSW-Only\" >","");
            result = result.Replace("<font class=\"ms-rteCustom-YellowBorderBox\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-YellowBorderBox\" >", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureNormal\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureNormal\" >", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureGood\">", "");
            result = result.Replace("<font  class=\"ms-rteCustom-FigureGood\">", "");
            result = result.Replace("<font  class=\"ms-rteCustom-FigureBad\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureBad\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureBad\" >", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureGood\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureGood\" >", "");
            result = result.Replace("<font class=\"ms-rteCustom-FigureBad\">", "");
            result = result.Replace("<font class=\"ms-rteCustom-CodeArea\">", "");
            result = result.Replace("<p class=\"ms-rteCustom-CodeArea\">", "");

            result = result.Replace("<font size=\"2\">", "");
            result = result.Replace("<font face=\"Verdana, sans-serif\">", "");
            result = result.Replace("<font face=\"Verdana\">", "");
            result = result.Replace("<font face=\"Calibri\">", "");
            result = result.Replace("<font color=\"#000080\">", "");
            result = result.Replace("<font color=\"#333333\">", "");
            result = result.Replace("<font color=\"#3a66cc\">", "");
            result = result.Replace("<font color=\"#555555\">", "");
            result = result.Replace("<font style=\"color&#58;#000000;\">", "");
            result = result.Replace("<font color=\"#ff0000\">", "");
            result = result.Replace("<font color=\"#333333\" style=\"line-height:18px;\">", "");
            result = result.Replace("<span style=\"font -family:verdana, sans-serif;font-size:9pt;\">","");
            result = result.Replace("<font style=\"background-color:#ff0000;\">","");
            result = result.Replace("<font style=\"background-color:#ffff00;\">", "");
            result = result.Replace("<font color=\"#0000ff\" face=\"Consolas\" size=\"2\">", "");
            result = result.Replace("<font color=\"#008080\" face=\"Consolas\" size=\"2\">", "");
            result = result.Replace("<font color=\"#444444\">", "");
            */

            return result;
        }
    }
}
