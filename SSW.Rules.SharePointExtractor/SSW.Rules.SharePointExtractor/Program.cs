using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Social;
using Newtonsoft.Json;
using Serilog;
using SSW.Rules.SharePointExtractor.MdWriter;
using SSW.Rules.SharePointExtractor.Models;
using SSW.Rules.SharePointExtractor.SpImporter;

namespace SSW.Rules.SharePointExtractor
{
    public class Program
    {

        /// <summary>
        /// set to read from a json file instead of slowly importing from sharepoint
        /// set via command argument ReadFile=xxx
        /// </summary>
        public static string ReadFile = null;

        /// <summary>
        /// set to write sharepoint data to a json file
        /// set via command argument WriteFile=xxx
        /// does not make much sense to read and write in the same run!
        /// </summary>
        public static string WriteFile = null;


        public static void Main(string[] args)
        {
            // TODO - could bring in a full library for cli parameters & help text, but this works well enough for now.
            var readFileArg = args.FirstOrDefault(a => a.StartsWith("ReadFile="));
            if (readFileArg != null)
            {
                ReadFile = readFileArg.Substring("ReadFile=".Length);
                Console.Out.WriteLine($"Will read data from {ReadFile}");
            }
            
            var writeFileArg = args.FirstOrDefault(a => a.StartsWith("WriteFile="));
            if (writeFileArg != null)
            {
                WriteFile = writeFileArg.Substring("WriteFile=".Length);
                Console.Out.WriteLine($"Will write data to {WriteFile}");
            }


            try
            {
                using (var scope = BuildServiceProvider().CreateScope())
                {
                    var log = scope.ServiceProvider.GetService<ILogger<Program>>();
                    log.LogInformation("Starting SharePoint Extractor...");


                    // Fetch Sharepoint Data 
                    var importer = scope.ServiceProvider.GetService<ISpImporter>();
                    var data = importer.Import();

                    // write to file if configured
                    if (WriteFile != null)
                    {
                        var serialized = JsonConvert.SerializeObject(data,
                            new JsonSerializerSettings()
                                {PreserveReferencesHandling = PreserveReferencesHandling.Objects});
                        using (var writer = new StreamWriter(WriteFile))
                        {
                            writer.Write(serialized);
                        }

                        log.LogInformation("wrote to json file");
                    }


                    // DEBUG Uncomment to log details of ruleset
                    //LogRuleSetDetails(log, data);


                    // write to markdown
                    var mdWriter = scope.ServiceProvider.GetService<IMdWriter>();
                    mdWriter.WriteMarkdown(data);
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


        public static IServiceProvider BuildServiceProvider()

        {
            var services = new ServiceCollection();
            var appSettings = ApplicationSettings.LoadConfig();

            Log.Logger = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .WriteTo.Console().WriteTo.File("ExtractionLogs.txt")
              .CreateLogger();

            services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

            services.AddSingleton<ApplicationSettings>(appSettings);

            // are we importing from sharepoint or loading from a file?
            if (ReadFile != null)
            {
                services.AddSingleton<ISpImporter>(new FileImporter(ReadFile));
            }
            else
            {
                services.AddSingleton<ISpImporter, SpImporter.SpImporter>();
            }

            if (appSettings.MdWriterConfig != null) services.AddSingleton(appSettings.MdWriterConfig);
            services.AddSingleton<IMdWriter, MdWriter.MdWriter>();

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
