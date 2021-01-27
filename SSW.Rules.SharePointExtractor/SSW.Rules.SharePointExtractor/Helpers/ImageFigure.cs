using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SSW.Rules.SharePointExtractor.Helpers
{
    public class ImageFigure
    {
        public static string Create(string type, string figCaption, string imgSrc)
        {
            var imageFigure = "";

            if (String.IsNullOrEmpty(type)) {
                imageFigure = "<br>![" + figCaption + "](" + imgSrc + ")  <br>";
            } else {
                imageFigure = "<br><br>";
                imageFigure += "::: " + type + "  <br>";
                imageFigure += "![" + figCaption + "](" + imgSrc + ")  <br>";
                imageFigure += ":::<br>";
            }

            return imageFigure;
        }
    }
}
