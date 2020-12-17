using Microsoft.Extensions.Configuration;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public static class Config
    {

        public static ApplicationConfig BuildApplicationConfig()
        {
            var config = BuildConfig();
            return new ApplicationConfig(config);
        }
        
        
        public static IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.local.json")
                .AddEnvironmentVariables()
                .Build();
        }
        
        
    }
}