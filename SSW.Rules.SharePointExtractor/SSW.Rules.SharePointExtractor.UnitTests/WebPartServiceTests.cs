﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using SSW.Rules.SharePointExtractor.SpRulesListsService;
using SSW.Rules.SharePointExtractor.SpWebPartService;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class WebPartServiceTests
    {

        private readonly SharepointConfig _cfg;

        public WebPartServiceTests()
        {
            _cfg = Config.BuildApplicationConfig().SharepointConfig;
        }



        [Fact]
        public void ShouldGetWebPart()
        {

            var svc = new WebPartPagesWebServiceSoapClient();
            svc.ClientCredentials.Windows.ClientCredential = new System.Net.NetworkCredential(_cfg.Username, _cfg.Password, _cfg.Domain);
            
            var xx = svc.GetWebPart2("/Pages/Rules-to-Successful-Projects.aspx", new Guid("abedc3a2-8283-4264-87c4-31201fe8274b"), SpWebPartService.Storage.Shared, SPWebServiceBehavior.Version3);
            xx.Should().NotBeNull();

        }





    }
}
