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
            //TODO: Fix line breaks
            var imageFigure = "";

            if (String.IsNullOrEmpty(type)) {
                imageFigure = "![" + figCaption + "](" + imgSrc + ")  ";
            } else {
                imageFigure = "  ";
                imageFigure += "::: " + type + "  ";
                imageFigure += "![" + figCaption + "](" + imgSrc + ")  ";
                imageFigure += ":::   ";
            }

            return imageFigure;
        }
    }
}
