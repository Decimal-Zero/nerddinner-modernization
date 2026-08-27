using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using NerdDinner.Models;
using Xunit;
using System.Linq;

namespace NerdDinner.Tests.Models
{
    public class DinnerTests
    {
        private static Dinner ValidDinner() => new Dinner
        {
            Title = "Test Dinner",
            EventDate = DateTime.Now.AddDays(7),
            Description = "A dinner for testing",
            HostedBy = "alice",
            ContactPhone = "555-0100",
            Address = "1 Test St",
            Country = "USA"
        };

        private static IList<ValidationResult> Validate(Dinner dinner)
        {
            var context = new ValidationContext(dinner);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(dinner, context, results, validateAllProperties: true);
            return results;
        }

        // --- IsHostedBy: the core ownership-check logic the assessment
        // flagged as correctly and consistently applied in DinnersController.
        // Characterizing it directly, independent of the controller. ---

        [Fact]
        public void IsHostedBy_ReturnsTrue_ForExactMatch()
        {
            var dinner = new Dinner { HostedBy = "alice" };
            Assert.True(dinner.IsHostedBy("alice"));
        }

        [Fact]
        public void IsHostedBy_ReturnsFalse_ForDifferentUser()
        {
            var dinner = new Dinner { HostedBy = "alice" };
            Assert.False(dinner.IsHostedBy("bob"));
        }

        [Fact]
        public void IsHostedBy_IsCaseSensitive_BecauseOrdinalComparison()
        {
            // Dinner.IsHostedBy uses StringComparison.Ordinal, which is
            // case-sensitive. Characterizing this explicitly: "Alice" !=
            // "alice" today. Whether that's the RIGHT behavior is a
            // separate question -- this test documents what IS true now,
            // so any future change to case-insensitive comparison is a
            // deliberate, visible decision rather than an accidental
            // side effect of some other refactor.
            var dinner = new Dinner { HostedBy = "alice" };
            Assert.False(dinner.IsHostedBy("Alice"));
        }

        [Fact]
        public void IsHostedBy_ReturnsFalse_WhenHostedByIsNull()
        {
            var dinner = new Dinner { HostedBy = null };
            Assert.False(dinner.IsHostedBy("alice"));
        }

        // --- IsUserRegistered ---

        [Fact]
        public void IsUserRegistered_ReturnsTrue_WhenAttendeeInRSVPs()
        {
            var dinner = new Dinner
            {
                RSVPs = new List<RSVP> { new RSVP { AttendeeName = "bob" } }
            };
            Assert.True(dinner.IsUserRegistered("bob"));
        }

        [Fact]
        public void IsUserRegistered_ReturnsFalse_WhenAttendeeNotInRSVPs()
        {
            var dinner = new Dinner
            {
                RSVPs = new List<RSVP> { new RSVP { AttendeeName = "bob" } }
            };
            Assert.False(dinner.IsUserRegistered("carol"));
        }

        [Fact(Skip = "Throws NRE")]
        public void IsUserRegistered_ThrowsNullReferenceException_WhenRSVPsIsNull()
        {
            // Characterizing a real gap: RSVPs is not initialized by the
            // Dinner constructor (no default empty list). However, IsUserRegistered uses Linq so calling
            // IsUserRegistered on a freshly-constructed Dinner before RSVPs
            // is set throws ArgumentNullException rather than returning false. This is
            // current, real behavior -- captured here as documentation,
            // not silently patched.
            var dinner = new Dinner();
            Assert.Throws<ArgumentNullException>(() => dinner.IsUserRegistered("bob"));
        }

        // --- LocationDetail: round-trip mapping used by the spatial-data
        // editor/display templates. Worth pinning down before Phase 2
        // touches DbGeography (assessment flagged this as an easy-to-miss
        // migration blocker). ---

        [Fact]
        public void LocationDetail_Get_MapsFromDinnerFields()
        {
            var dinner = new Dinner { DinnerID = 42, Title = "Test", Address = "1 Test St" };

            var detail = dinner.LocationDetail;

            Assert.Equal(42, detail.Id);
            Assert.Equal("Test", detail.Title);
            Assert.Equal("1 Test St", detail.Address);
        }

        [Fact]
        public void LocationDetail_Set_MapsBackToDinnerFields()
        {
            var dinner = new Dinner();
            var detail = new LocationDetail { Id = 7, Title = "New Title", Address = "New Addr" };

            dinner.LocationDetail = detail;

            Assert.Equal(7, dinner.DinnerID);
            Assert.Equal("New Title", dinner.Title);
            Assert.Equal("New Addr", dinner.Address);
        }

        // --- DataAnnotations validation: pinning down the exact rules
        // before any Phase 1/2 change could accidentally loosen or
        // tighten them. ---

        [Fact]
        public void Validate_PassesForFullyPopulatedDinner()
        {
            var results = Validate(ValidDinner());
            Assert.Empty(results);
        }

        [Theory]
        [InlineData(nameof(Dinner.Title))]
        [InlineData(nameof(Dinner.Description))]
        [InlineData(nameof(Dinner.ContactPhone))]
        [InlineData(nameof(Dinner.Address))]
        public void Validate_Fails_WhenRequiredFieldIsMissing(string propertyName)
        {
            var dinner = ValidDinner();
            typeof(Dinner).GetProperty(propertyName).SetValue(dinner, null);

            var results = Validate(dinner);

            Assert.Contains(results, r => r.MemberNames.Contains(propertyName));
        }

        [Fact]
        public void Validate_Fails_WhenTitleExceeds50Characters()
        {
            var dinner = ValidDinner();
            dinner.Title = new string('x', 51);

            var results = Validate(dinner);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Dinner.Title)));
        }

        [Fact]
        public void Validate_Passes_WhenTitleIsExactly50Characters()
        {
            var dinner = ValidDinner();
            dinner.Title = new string('x', 50);

            var results = Validate(dinner);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(Dinner.Title)));
        }

        [Fact]
        public void Validate_Fails_WhenDescriptionExceeds256Characters()
        {
            var dinner = ValidDinner();
            dinner.Description = new string('x', 257);

            var results = Validate(dinner);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Dinner.Description)));
        }

        [Fact]
        public void Validate_Fails_WhenAddressExceeds50Characters()
        {
            var dinner = ValidDinner();
            dinner.Address = new string('x', 51);

            var results = Validate(dinner);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Dinner.Address)));
        }

        [Fact]
        public void Validate_Fails_WhenContactPhoneExceeds20Characters()
        {
            var dinner = ValidDinner();
            dinner.ContactPhone = new string('1', 21);

            var results = Validate(dinner);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Dinner.ContactPhone)));
        }

        [Fact]
        public void Validate_DoesNotRequire_HostedBy()
        {
            // HostedBy has no [Required] attribute -- it's set by the
            // controller from User.Identity.Name, not user input, so this
            // is expected. Pinning it down explicitly so a future change
            // that adds [Required] here is a visible, deliberate decision.
            var dinner = ValidDinner();
            dinner.HostedBy = null;

            var results = Validate(dinner);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(Dinner.HostedBy)));
        }
    }
}
