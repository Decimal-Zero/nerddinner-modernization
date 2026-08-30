using System;
using System.Web.Mvc;
using NerdDinner.Controllers;
using NerdDinner.Models;
using NerdDinner.Tests.TestSupport;
using X.PagedList;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    // Seed data (see TestDatabase.cs):
    //   "Past Dinner"          - EventDate -7d,  HostedBy "alice"
    //   "Alice's Dinner"       - EventDate +7d,  HostedBy "alice"
    //   "Bob's Dinner"         - EventDate +14d, HostedBy "bob", 2 RSVPs (bob, carol)

    [Collection("NerdDinner LocalDB collection")]
    public class DinnersControllerTests
    {
        public DinnersControllerTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        // --- Index: only future dinners, ordered by date ---

        [Fact]
        public void Index_ExcludesPastDinners()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));

            var result = controller.Index(page: null) as ViewResult;
            var model = (IPagedList<Dinner>)result.Model;

            Assert.DoesNotContain(model, d => d.Title == "Past Dinner");
        }

        [Fact]
        public void Index_OrdersUpcomingDinnersByEventDateAscending()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));

            var result = controller.Index(page: null) as ViewResult;
            var model = (IPagedList<Dinner>)result.Model;

            // "Alice's Dinner" (+7d) should come before "Bob's Dinner" (+14d)
            var titles = new System.Collections.Generic.List<string>();
            foreach (var d in model) titles.Add(d.Title);

            Assert.True(titles.IndexOf("Alice's Dinner") < titles.IndexOf("Bob's Dinner"));
        }

        // --- Details ---

        [Fact]
        public void Details_ReturnsHttpNotFound_ForNonexistentId()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));

            var result = controller.Details(id: 999999);

            Assert.IsType<HttpNotFoundResult>(result);
        }

        // --- Create (GET): prefills from the current user ---

        [Fact]
        public void CreateGet_PrefillsHostedByFromCurrentUser()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");

            var result = controller.Create() as ViewResult;
            var dinner = (Dinner)result.Model;

            Assert.Equal("alice", dinner.HostedBy);
        }

        [Fact]
        public void CreateGet_DefaultsEventDateToOneWeekFromNow()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");

            var result = controller.Create() as ViewResult;
            var dinner = (Dinner)result.Model;

            // Loose bound rather than exact equality, since "now" ticks
            // forward between the controller call and this assertion.
            Assert.InRange(dinner.EventDate, DateTime.Now.AddDays(6.9), DateTime.Now.AddDays(7.1));
        }

        // --- Create (POST): invalid ModelState redisplays the form ---

        [Fact]
        public void CreatePost_ReturnsViewWithModel_WhenModelStateInvalid()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");
            controller.ModelState.AddModelError("Title", "Title is required");

            var dinner = new Dinner { Title = null };
            var result = controller.Create(dinner) as ViewResult;

            Assert.NotNull(result);
            Assert.Same(dinner, result.Model);
        }

        // --- Edit: ownership check (the core finding from the assessment) ---

        [Fact]
        public void EditGet_ReturnsInvalidOwnerView_WhenCurrentUserIsNotHost()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("bob");

            // "Alice's Dinner" is hosted by alice; find its id via a
            // throwaway query since the fixture doesn't expose ids directly.
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Edit(dinnerId) as ViewResult;

            Assert.Equal("InvalidOwner", result.ViewName);
        }

        [Fact]
        public void EditGet_ReturnsDinnerView_WhenCurrentUserIsHost()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Edit(dinnerId) as ViewResult;

            // Default view (empty ViewName), not "InvalidOwner"
            Assert.True(string.IsNullOrEmpty(result.ViewName));
        }

        [Fact]
        public void EditGet_ReturnsHttpNotFound_ForNonexistentId()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");

            var result = controller.Edit(id: 999999);

            Assert.IsType<HttpNotFoundResult>(result);
        }

        // --- Delete: same ownership check pattern ---

        [Fact]
        public void DeleteGet_ReturnsInvalidOwnerView_WhenCurrentUserIsNotHost()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("bob");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Delete(dinnerId) as ViewResult;

            Assert.Equal("InvalidOwner", result.ViewName);
        }

        [Fact]
        public void DeleteConfirmed_ThrowsNullReferenceException_ForNonexistentId()
        {
            // Real, pre-existing bug, characterized rather than fixed here:
            // DeleteConfirmed calls db.Dinners.Find(id) and immediately
            // calls dinner.IsHostedBy(...) with no null check -- unlike
            // the GET Delete action, which does check for null and returns
            // HttpNotFound. A POST to /Dinners/Delete/{missing-id} throws
            // an unhandled NRE today rather than a clean 404. This is
            // exactly the kind of thing DL-004 says to capture honestly:
            // current (bad) behavior, not the behavior we'd prefer it had.
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));
            controller.SetFakeUser("alice");

            Assert.Throws<NullReferenceException>(() => controller.DeleteConfirmed(id: 999999));
        }

        // --- WebSlice actions: simple enough to pin down directly ---

        [Fact]
        public void WebSlicePopular_OrdersByRSVPCountDescending_AndExcludesPastDinners()
        {
            var controller = new DinnersController(new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")));

            var result = controller.WebSlicePopular() as ViewResult;
            var model = (System.Collections.Generic.IEnumerable<Dinner>)result.Model;

            var titles = new System.Collections.Generic.List<string>();
            foreach (var d in model) titles.Add(d.Title);

            Assert.DoesNotContain("Past Dinner", titles);
            // "Bob's Dinner" has 2 RSVPs vs. "Alice's Dinner"'s 0, so it
            // should be first if both are present.
            if (titles.Contains("Bob's Dinner") && titles.Contains("Alice's Dinner"))
            {
                Assert.True(titles.IndexOf("Bob's Dinner") < titles.IndexOf("Alice's Dinner"));
            }
        }

        private static int FindDinnerIdByTitle(string title)
        {
            using (var db = new NerdDinnerContext(TestConnectionStrings.Get("NerdDinnerContext")))
            {
                var dinner = System.Linq.Enumerable.First(db.Dinners, d => d.Title == title);
                return dinner.DinnerID;
            }
        }
    }
}
