using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Social;
using Newtonsoft.Json;
using SSW.Rules.SharePointExtractor.MdWriter;
using SSW.Rules.SharePointExtractor.Models;
using SSW.Rules.SharePointExtractor.SpImporter;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace SSW.Rules.SharePointExtractor
{
    public class Program
    {

        public static IConfiguration Configuration { get; set; }

        public static ApplicationConfig ApplicationConfig { get; set; }
        

        public static void Main(string[] args)
        {

            Configuration = BuildConfig(args);
            ApplicationConfig = new ApplicationConfig(Configuration);
            
            try
            {
                using (var scope = BuildServiceProvider().CreateScope())
                {
                    var log = scope.ServiceProvider.GetService<ILogger<Program>>();

                    SpRulesDataSet data = null;
                    
                    if (ApplicationConfig.SharepointExtractEnabled)
                    {
                        log.LogInformation("Starting SharePoint Extractor...");
                        var importer = scope.ServiceProvider.GetService<SpImporter.SpImporter>();
                        data = importer.Import();
                        
                        var serialized = JsonConvert.SerializeObject(data,
                            new JsonSerializerSettings()
                                {PreserveReferencesHandling = PreserveReferencesHandling.Objects});
                        using (var writer = new StreamWriter(ApplicationConfig.DataFile))
                        {
                            writer.Write(serialized);
                        }
                        log.LogInformation("wrote to json file");
                    }
                    else
                    {
                        log.LogInformation("loading previously extracted data from file");
                        var fileImporter = scope.ServiceProvider.GetService<FileImporter>();
                        data = fileImporter.Import();
                    }

                    if (ApplicationConfig.MdWriterEnabled)
                    {
                        var mdWriter = scope.ServiceProvider.GetService<IMdWriter>();
                        mdWriter.WriteMarkdown(data);
                    }

                }
            }
            catch (Exception ex)
            {
                
                Console.Out.WriteLine("Error! "+ex.Message);
                Console.Out.WriteLine("");
                Console.Out.WriteLine(ex.StackTrace);
            }
            finally
            {
                // hack - sleep to ensure all loggers have flushed
                Thread.Sleep(2000);
            }
        }

        private static void LogRuleSetDetails(ILogger<Program> log, SpRulesDataSet data)
        {
            log.LogInformation($"loaded {data.Rules.Count} rules across {data.Categories.Count} categories");
            foreach (var parent in data.ParentCategories)
            {
                log.LogInformation("= {ParentTitle}", parent.Title);
                foreach (var cat in parent.Categories)
                {
                    log.LogInformation("=== {CatTitle}", cat.Title);
                    foreach (var rule in cat.Rules)
                    {
                        log.LogInformation("======= {RuleTitle}", rule.Title);
                    }
                }
            }
        }

        public static IConfiguration BuildConfig(string[] args)
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.local.json")
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
        }

        public static IServiceProvider BuildServiceProvider()

        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddConsole());

            services.AddSingleton<ApplicationConfig>(ApplicationConfig);
            if (ApplicationConfig.SharepointExtractEnabled)
            {
                services.AddSingleton(ApplicationConfig.SharepointConfig);
                services.AddSingleton<SpImporter.SpImporter>();
            }
            else
            {
                services.AddSingleton(new FileImporter(ApplicationConfig.DataFile));
            }
            
            if (ApplicationConfig.MdWriterEnabled)
            {
                services.AddSingleton(ApplicationConfig.MdWriterConfig);
                services.AddSingleton<IMdWriter, MdWriter.MdWriter>();
            }
            return services.BuildServiceProvider();

        }

        private static void TestConnection()
        {
            using (var ctx = new ClientContext("https://rules.ssw.com.au/"))
            {

                NetworkCredential networkCredential = new NetworkCredential("Username", "Password", "SSW2000");
                ctx.Credentials = networkCredential;
                ctx.ExecutingWebRequest += new EventHandler<WebRequestEventArgs>(clientContext_ExecutingWebRequest);

                var web = ctx.Web;
                ctx.Load(web);
                ctx.ExecuteQuery();

                Console.Out.WriteLine($"Got Title {web.Title}");

            }

           
        }


        static void clientContext_ExecutingWebRequest(object sender, WebRequestEventArgs e)
        {
            try
            {
                e.WebRequestExecutor.WebRequest.Headers.Add("X-FORMS_BASED_AUTH_ACCEPTED", "f");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
