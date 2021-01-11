using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using SSW.Rules.SharePointExtractor.Models;

namespace SSW.Rules.SharePointExtractor.SpImporter
{

    /// <summary>
    /// this lets us skip the ugly and slow sharepoint business and load a previously saved dataset from a json file.
    /// </summary>
    public class FileImporter : ISpImporter
    {

        private readonly string _filePath;

        public FileImporter(string filePath)
        {
            _filePath = filePath;
        }

        public SpRulesDataSet Import()
        {
            using (var reader = new StreamReader(_filePath))
            {
                var json = reader.ReadToEnd();
                json = Helpers.EncodedHtmlTags.Encode(json);
                return JsonConvert.DeserializeObject<SpRulesDataSet>(json, new JsonSerializerSettings() {PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            }
        }
    }
}
