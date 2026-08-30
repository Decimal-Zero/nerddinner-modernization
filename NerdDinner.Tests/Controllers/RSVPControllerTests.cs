using Microsoft.AspNetCore.Mvc;
using NerdDinner.Controllers;
using NerdDinner.Tests.TestSupport;
using Xunit;

namespace NerdDinner.Tests.Controllers
{
    // Ported from NerdDinner.Tests.Controllers.RSVPControllerTests (M9,
    // decision-log.md DL-028) -- same behaviors, including the preserved
    // RegisterForDinner NRE-on-missing-dinner-id gap.
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
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("dave2");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.Register(dinnerId) as RedirectToActionResult;

            Assert.Equal("Details", result.ActionName);
            Assert.Equal("Dinners", result.ControllerName);
            Assert.Equal(dinnerId, result.RouteValues["id"]);
        }

        [Fact]
        public void Register_AddsRSVP_ForNewAttendee()
        {
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("erin");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            controller.Register(dinnerId);

            using var verifyDb = TestDatabaseFixture.CreateContext();
            var dinner = verifyDb.Dinners.Find(dinnerId);
            Assert.Contains(dinner.RSVPs, r => r.AttendeeName == "erin");
        }

        [Fact]
        public void Register_IsIdempotent_DoesNotDuplicateRSVP_ForAlreadyRegisteredAttendee()
        {
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("bob");
            int dinnerId = FindDinnerIdByTitle("Bob's Dinner");

            controller.Register(dinnerId);

            using var verifyDb = TestDatabaseFixture.CreateContext();
            var dinner = verifyDb.Dinners.Find(dinnerId);
            int bobCount = dinner.RSVPs.Count(r => r.AttendeeName == "bob");
            Assert.Equal(1, bobCount);
        }

        [Fact]
        public void Register_ThrowsNullReferenceException_ForNonexistentDinnerId()
        {
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("dave2");

            Assert.Throws<NullReferenceException>(() => controller.Register(id: 999999));
        }

        [Fact]
        public void CancelAjax_RemovesRSVP_WhenAttendeeIsRegistered()
        {
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("carol");
            int dinnerId = FindDinnerIdByTitle("Bob's Dinner");

            controller.CancelAjax(dinnerId);

            using var verifyDb = TestDatabaseFixture.CreateContext();
            var dinner = verifyDb.Dinners.Find(dinnerId);
            Assert.DoesNotContain(dinner.RSVPs, r => r.AttendeeName == "carol");
        }

        [Fact]
        public void CancelAjax_ReturnsFriendlyMessage_EvenWhenAttendeeWasNotRegistered()
        {
            using var db = TestDatabaseFixture.CreateContext();
            var controller = new RSVPController(db);
            controller.SetFakeUser("nobody-registered");
            int dinnerId = FindDinnerIdByTitle("Alice's Dinner");

            var result = controller.CancelAjax(dinnerId) as ContentResult;

            Assert.Equal("Sorry you can't make it!", result.Content);
        }

        private static int FindDinnerIdByTitle(string title)
        {
            using var db = TestDatabaseFixture.CreateContext();
            return db.Dinners.First(d => d.Title == title).DinnerID;
        }
    }
}
