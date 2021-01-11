using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class HtmlDescriptionList
    {
        public static string Process(string html)
        {
            string result = html;

            result = HtmlHelper.ReplaceDlTagsWithImageFigures(result);
            //Clean HTML
            /*
            result = result.Replace("<dl class='image'>", "");
            result = result.Replace("<dl class=\"image\">", "");
            result = result.Replace("<dl class=\"Image\">", "");
            result = result.Replace("<dl class=\"image\" style=\"float:right;clear:right;\">", "");
            result = result.Replace("<dl class=\"image\" style=\"float:right;clear:right;width:207px;\">", "");
            result = result.Replace("<dl class=\"image\" style=\"padding:15px;width:230px;float:right;\">", "");
            result = result.Replace("<dl class='badImage'>", "");
            result = result.Replace("<dl class=\"badImage\">", "");
            result = result.Replace("<dl class=\"goodImage\">", "");
            result = result.Replace("<dl class=\"good\">", "");
            result = result.Replace("<dl class=\"bad\">", "");
            result = result.Replace("<dl class=\"code\">", "");
            result = result.Replace("<dl class=\"badCode\">", "");
            result = result.Replace("<dl class=\"goodCode\">", "");
            result = result.Replace("<dl class=\"ssw15-rteElement-ImageArea\">", "");
            result = result.Replace("<dl class=\"image\" style=\"text-decoration:line-through;\">", "");
            result = result.Replace("<dt class=\"ssw-rteStyle-ImageArea\">", "");
            result = result.Replace("<dt style=\"border:none;\">", "");
            result = result.Replace("<dt class=\"Code\">", "");
            result = result.Replace("<dl class=\"Code\">", "");
            result = result.Replace("<dl class=\"bad\" style=\"margin:0px;padding-top:10px;padding-bottom:10px;padding-left:20px;font-family:arial, helvetica, sans-serif;line-height:17px;\">","");
            result = result.Replace("<dl class=\"good\" style=\"margin:0px;padding-top:10px;padding-bottom:10px;padding-left:20px;\">", "");
            result = result.Replace("<dt style=\"border:currentcolor;color:#000000;line-height:17px;font-family:verdana, sans-serif;font-size:12px;\">","");
            result = result.Replace("<dl class=\"image\" style=\"padding-right:1.2em;padding-left:1.2em;font-size:1em;\">","");
            result = result.Replace("<dl class=\"bad\" style=\"margin:0px;line-height:17px;padding-top:10px;padding-bottom:10px;padding-left:20px;font-family:arial, helvetica, sans-serif;\">","");

            result = result.Replace("<dl>", "");
            result = result.Replace("<dt>", "&lt;dt&gt;");
            result = result.Replace("</dt>", "&lt;/dt&gt;");
            */

            return result;
        }
    }
}
