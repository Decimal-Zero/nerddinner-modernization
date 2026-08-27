using System.Web.Mvc;
using NerdDinner.Controllers;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_SetsWelcomeMessage()
        {
            var controller = new HomeController();

            var result = controller.Index() as ViewResult;

            Assert.Equal(
                "Organizing the world's nerds and helping them eat in packs.",
                result.ViewBag.Message);
        }

        [Fact]
        public void Index_ReturnsDefaultView()
        {
            var controller = new HomeController();

            var result = controller.Index() as ViewResult;

            Assert.True(string.IsNullOrEmpty(result.ViewName));
        }

        [Fact]
        public void About_ReturnsDefaultView()
        {
            var controller = new HomeController();

            var result = controller.About() as ViewResult;

            Assert.True(string.IsNullOrEmpty(result.ViewName));
        }
    }
}
