using Microsoft.AspNetCore.Mvc;
using NerdDinner.Proxy.Controllers;
using NerdDinner.Proxy.Helpers;
using NerdDinner.Proxy.Models;
using NerdDinner.Proxy.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Proxy.Tests.Controllers
{
    // Ported from NerdDinner.Tests.Controllers.DinnersControllerTests (M9,
    // decision-log.md DL-028) -- same behaviors characterized, including
    // the preserved DeleteConfirmed NRE-on-missing-id gap (DL-004: capture
    // current behavior, don't fix it mid-port).
    [Collection("NerdDinner.Proxy LocalDB collection")]
    public class DinnersControllerTests
    {
        public DinnersControllerTests(ProxyTestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        [Fact]
        public void Index_ExcludesPastDinners()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);

            var result = controller.Index(page: null) as ViewResult;
            var model = (IPagedList<Dinner>)result.Model;

            Assert.DoesNotContain(model, d => d.Title == "Past Dinner");
        }

        [Fact]
        public void Index_OrdersUpcomingDinnersByEventDateAscending()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);

            var result = controller.Index(page: null) as ViewResult;
            var model = (IPagedList<Dinner>)result.Model;

            var titles = new List<string>();
            foreach (var d in model) titles.Add(d.Title);

            Assert.True(titles.IndexOf("Alice's Dinner") < titles.IndexOf("Bob's Dinner"));
        }

        [Fact]
        public void Details_ReturnsNotFound_ForNonexistentId()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);

            var result = controller.Details(id: 999999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void CreateGet_PrefillsHostedByFromCurrentUser()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("alice");

            var result = controller.Create() as ViewResult;
            var dinner = (Dinner)result.Model;

            Assert.Equal("alice", dinner.HostedBy);
        }

        [Fact]
        public void CreatePost_AddsDinnerWithHostAsFirstRSVP()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("erin");

            var dinner = new Dinner
            {
                Title = "Erin's New Dinner",
                EventDate = DateTime.Now.AddDays(3),
                Description = "A brand new dinner",
                ContactPhone = "555-0199",
                Address = "5 New St",
                Country = "USA"
            };

            controller.Create(dinner);

            using var verifyDb = ProxyTestDatabaseFixture.CreateContext();
            var saved = verifyDb.Dinners.First(d => d.Title == "Erin's New Dinner");
            Assert.Equal("erin", saved.HostedBy);
            Assert.Contains(saved.RSVPs, r => r.AttendeeName == "erin");
        }

        [Fact]
        public void CreatePost_ReturnsViewWithModel_WhenModelStateInvalid()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("alice");
            controller.ModelState.AddModelError("Title", "Title is required");

            var dinner = new Dinner { Title = null };
            var result = controller.Create(dinner) as ViewResult;

            Assert.NotNull(result);
            Assert.Same(dinner, result.Model);
        }

        [Fact]
        public void EditGet_ReturnsInvalidOwnerView_WhenCurrentUserIsNotHost()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("bob");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Edit(dinnerId) as ViewResult;

            Assert.Equal("InvalidOwner", result.ViewName);
        }

        [Fact]
        public void EditGet_ReturnsDefaultView_WhenCurrentUserIsHost()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("alice");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Edit(dinnerId) as ViewResult;

            Assert.True(string.IsNullOrEmpty(result.ViewName));
        }

        [Fact]
        public void DeleteGet_ReturnsInvalidOwnerView_WhenCurrentUserIsNotHost()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("bob");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Delete(dinnerId) as ViewResult;

            Assert.Equal("InvalidOwner", result.ViewName);
        }

        [Fact]
        public void DeleteConfirmed_ThrowsNullReferenceException_ForNonexistentId()
        {
            // Preserved from the legacy characterization, per DL-004 --
            // DeleteConfirmed calls db.Dinners.Find(id) and immediately
            // dereferences the result with no null check.
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);
            controller.SetFakeUser("alice");

            Assert.Throws<NullReferenceException>(() => controller.DeleteConfirmed(id: 999999));
        }

        [Fact]
        public void WebSlicePopular_OrdersByRSVPCountDescending_AndExcludesPastDinners()
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            var controller = new DinnersController(db);

            var result = controller.WebSlicePopular() as ViewResult;
            var model = (IEnumerable<Dinner>)result.Model;

            var titles = model.Select(d => d.Title).ToList();

            Assert.DoesNotContain("Past Dinner", titles);
            if (titles.Contains("Bob's Dinner") && titles.Contains("Alice's Dinner"))
            {
                Assert.True(titles.IndexOf("Bob's Dinner") < titles.IndexOf("Alice's Dinner"));
            }
        }

        private static int FindDinnerIdByTitle(string title)
        {
            using var db = ProxyTestDatabaseFixture.CreateContext();
            return db.Dinners.First(d => d.Title == title).DinnerID;
        }
    }
}
