using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using SSW.Rules.SharePointExtractor.SpRulesListsService;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class ListServiceTests
    {

        private readonly ApplicationConfig _appSettings;

        public ListServiceTests()
        {
            _appSettings = Config.BuildApplicationConfig();
        }



        [Fact]
        public void ShouldGetList()
        {

            var svc = new ListsSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.SharepointConfig.Username, _appSettings.SharepointConfig.Password, _appSettings.SharepointConfig.Domain);

            var xx = svc.GetList("TaxonomyHiddenList");
            xx.Should().NotBeNull();

        }




        [Fact]
        public void ShouldGetItems()
        {

            var svc = new ListsSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.SharepointConfig.Username, _appSettings.SharepointConfig.Password, _appSettings.SharepointConfig.Domain);

            XElement queryOptions = XElement.Parse(@"<QueryOptions/>");

            var xx = svc.GetListItems("Pages", "", null, null, null, queryOptions, null);
            Console.Out.WriteLine(xx.ToString());
            xx.Should().NotBeNull();

        }


        [Fact]
        public void ShouldGetItemVersions()
        {

            var svc = new ListsSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.SharepointConfig.Username, _appSettings.SharepointConfig.Password, _appSettings.SharepointConfig.Domain);
            var xx = svc.GetVersionCollection("Pages", "4", "Version");
            xx.Should().NotBeNull();

        }


        [Fact]
        public void ShouldGetItemVersionContent()
        {

            var svc = new ListsSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_appSettings.SharepointConfig.Username, _appSettings.SharepointConfig.Password, _appSettings.SharepointConfig.Domain);
            var xx = svc.GetVersionCollection("Pages", "4", "PublishingPageContent");

            xx.Should().NotBeNull();

        }




    }
}
