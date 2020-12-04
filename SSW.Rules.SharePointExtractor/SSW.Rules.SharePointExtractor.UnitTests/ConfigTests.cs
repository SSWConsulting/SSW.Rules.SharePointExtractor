using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace SSW.Rules.SharePointExtractor.UnitTests
{
    public class ConfigTests
    {

        [Fact]
        public void HelloXunit()
        {
            Assert.True(true);
        }


        [Fact]
        public void ShouldLoadConfig()
        {
            var settings = ApplicationSettings.LoadConfig();

            settings.Should().NotBeNull("expected a settings file: appsettings.local.json");
            settings.SharePointUrl.Should().NotBeNullOrEmpty();
            settings.Username.Should().NotBeNullOrEmpty();
            settings.Password.Should().NotBeNullOrEmpty();
        }

    }
}
