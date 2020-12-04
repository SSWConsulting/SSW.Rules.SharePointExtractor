using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SSW.Rules.SharePointExtractor.MdWriter;

namespace SSW.Rules.SharePointExtractor
{
    public class ApplicationSettings
    {

        public string SharePointUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Domain { get; set; }

        public MdWriterConfig MdWriterConfig { get; set; }




        public static ApplicationSettings LoadConfig(string path = "appsettings.local.json")
        {
            return JsonConvert.DeserializeObject<ApplicationSettings>(new StreamReader(path).ReadToEnd());
        }

    }
}
