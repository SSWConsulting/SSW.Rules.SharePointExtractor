using System.Dynamic;
using System.Security.Permissions;
using Microsoft.Extensions.Configuration;
using SSW.Rules.SharePointExtractor.MdWriter;

namespace SSW.Rules.SharePointExtractor
{
    public class ApplicationConfig
    {

        public ApplicationConfig(IConfiguration config)
        {
            DataFile = config["DataFile"];
            SharepointExtractEnabled = config.GetValue<bool>("SharepointExtractEnabled", false);
            MdWriterEnabled = config.GetValue<bool>("MdWriterEnabled");

            if (SharepointExtractEnabled)
            {
                SharepointConfig = config.GetSection("SharepointConfig").Get<SharepointConfig>();
            }

            if (MdWriterEnabled)
            {
                MdWriterConfig = config.GetSection("MdWriterConfig").Get<MdWriterConfig>();
            }
        }
        
        public string DataFile { get; set; }

        /// <summary>
        /// when true, we will extract data from sharepoint and write to data file
        /// when false, we will read from data file 
        /// </summary>
        public bool SharepointExtractEnabled { get; set; }

        /// <summary>
        /// when true, we will convert data to markdown files in a local git repository
        /// </summary>
        public bool MdWriterEnabled { get; set; }

        public SharepointConfig SharepointConfig { get; set; }
        
        public MdWriterConfig MdWriterConfig { get; set; }

    }

    public class SharepointConfig
    {
        public string SharePointUrl { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Domain { get; set; }
    }
}