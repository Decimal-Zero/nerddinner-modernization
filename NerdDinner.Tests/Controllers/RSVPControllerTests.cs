using System;
using System.Linq;
using System.Web.Mvc;
using NerdDinner.Controllers;
using NerdDinner.Models;
using NerdDinner.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    [Collection("NerdDinner LocalDB collection")]
    public class RSVPControllerTests
    {
        public RSVPControllerTests(TestDatabaseFixture fixture)
        {
            fixture.Reset();
        }

        [Fact]
        public void Register_RedirectsToDinnerDetails()
        {
            var controller = new RSVPController();
            controller.SetFakeUser("dave");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Register(dinnerId) as RedirectToRouteResult;

            Assert.Equal("Details", result.RouteValues["action"]);
            Assert.Equal("Dinners", result.RouteValues["controller"]);
            Assert.Equal(dinnerId, result.RouteValues["id"]);
        }

        [Fact]
        public void Register_AddsRSVP_ForNewAttendee()
        {
            var controller = new RSVPController();
            controller.SetFakeUser("erin");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            controller.Register(dinnerId);

            using (var db = new NerdDinnerContext())
            {
                var dinner = db.Dinners.Find(dinnerId);
                Assert.Contains(dinner.RSVPs, r => r.AttendeeName == "erin");
            }
        }

        [Fact]
        public void Register_IsIdempotent_DoesNotDuplicateRSVP_ForAlreadyRegisteredAttendee()
        {
            // "Bob's Dinner" is seeded with bob already RSVP'd.
            var controller = new RSVPController();
            controller.SetFakeUser("bob");
            int dinnerId = FindDinnerIdByTitle("Bob's Dinner");

            controller.Register(dinnerId); // bob registers again

            using (var db = new NerdDinnerContext())
            {
                var dinner = db.Dinners.Find(dinnerId);
                int bobCount = dinner.RSVPs.Count(r => r.AttendeeName == "bob");
                Assert.Equal(1, bobCount);
            }
        }

        [Fact]
        public void Register_ThrowsNullReferenceException_ForNonexistentDinnerId()
        {
            // Same pattern as DinnersController.DeleteConfirmed: no null
            // check after db.Dinners.Find(id) before calling
            // dinner.IsUserRegistered(...). A request for a dinner that
            // doesn't exist throws an unhandled NRE rather than a clean
            // error. Captured as-is, per DL-004.
            var controller = new RSVPController();
            controller.SetFakeUser("dave");

            Assert.Throws<NullReferenceException>(() => controller.Register(id: 999999));
        }

        [Fact]
        public void CancelAjax_RemovesRSVP_WhenAttendeeIsRegistered()
        {
            var controller = new RSVPController();
            controller.SetFakeUser("carol"); // seeded as an RSVP on Bob's Dinner
            int dinnerId = FindDinnerIdByTitle("Bob's Dinner");

            controller.CancelAjax(dinnerId);

            using (var db = new NerdDinnerContext())
            {
                var dinner = db.Dinners.Find(dinnerId);
                Assert.DoesNotContain(dinner.RSVPs, r => r.AttendeeName == "carol");
            }
        }

        [Fact]
        public void CancelAjax_ReturnsFriendlyMessage_EvenWhenAttendeeWasNotRegistered()
        {
            // dinner.RSVPs.SingleOrDefault(...) returning null is checked
            // (unlike the Register/DeleteConfirmed gaps above) -- this
            // action degrades gracefully. Worth confirming that
            // asymmetry explicitly.
            var controller = new RSVPController();
            controller.SetFakeUser("nobody-registered");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.CancelAjax(dinnerId) as ContentResult;

            Assert.Equal("Sorry you can't make it!", result.Content);
        }

        private static int FindDinnerIdByTitle(string title)
        {
            using (var db = new NerdDinnerContext())
            {
                var dinner = db.Dinners.First(d => d.Title == title);
                return dinner.DinnerID;
            }
        }
    }
}
