using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class SharePointClientTests
    {

        private readonly ApplicationSettings appSettings;

        public SharePointClientTests()
        {
            appSettings = ApplicationSettings.LoadConfig();
        }


        [Fact]
        public void ShouldConnectToSite()
        {
            using (var ctx = SpImporter.SpImporter.CreateClientContext(appSettings))
            {
                var web = ctx.Web;
                ctx.Load(web);
                ctx.ExecuteQuery();

                ctx.Web.Title.Should().NotBeNullOrEmpty("expect to fetch title details from the sharepoint site");
            }
        }


        [Fact]
        public void ShouldLoadTaxonomyHiddenList()
        {
            using (var ctx = SpImporter.SpImporter.CreateClientContext(appSettings))
            {
                var items = SpImporter.SpImporter.GetAllItems(ctx, "TaxonomyHiddenList");
                items.Should().NotBeNull();
                items.Count.Should().BeGreaterOrEqualTo(5);
            }
        }

        [Fact]
        public void ShouldGetCategoryContent()
        {
            using (var ctx = SpImporter.SpImporter.CreateClientContext(appSettings))
            {
                var web = ctx.Web;

                List oList = ctx.Web.Lists.GetByTitle("Pages");
                ctx.Load(oList);
                ctx.ExecuteQuery();


                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = @"<View><Query><Where>
  <Eq>
    <FieldRef Name='ContentType'/>
    <Value Type='Computed'>RuleSummaryPage</Value>
  </Eq>
</Where></Query><RowLimit>5000</RowLimit></View>";
                ListItemCollection items = oList.GetItems(camlQuery);
                ctx.Load(items);
                ctx.ExecuteQuery();

                var allContent = new Dictionary<string, string>();


                items.Should().NotBeEmpty();

                foreach (var item in items)
                {
                    var contentType = item.ContentType;
                    ctx.Load(item);
                    ctx.Load(contentType);
                    var file = ctx.Web.GetFileByServerRelativeUrl($"/Pages/{item["FileLeafRef"]}");
                    ctx.Load(file);
                    ctx.ExecuteQuery();
                    if (item.ContentType.Name == "RuleSummaryPage")
                    {
                        var title = item["Title"]?.ToString();
                        var publishingPageContent = item["PublishingPageContent"]?.ToString();
                        var ruleSummaryIntro = item["RuleSummaryIntro"]?.ToString();

                        if (title != null)
                        {
                            allContent[title] = ruleSummaryIntro + publishingPageContent;
                        }
                    }

                }

                // test we have content for "Rules to Successful Projects" - was missing in earlier runs 
                allContent["Rules to Successful Projects"].Should().NotBeNullOrEmpty();


                using (var sw = new StreamWriter(@"c:\temp\categories.json", false))
                {
                    sw.Write(JsonConvert.SerializeObject(allContent));
                    sw.Flush();
                }
            }
        }
       

    }
}
