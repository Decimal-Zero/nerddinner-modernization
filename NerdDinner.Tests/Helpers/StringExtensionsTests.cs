using NerdDinner.Helpers;
using Xunit;

namespace NerdDinner.Tests.Helpers
{
    public class StringExtensionsTests
    {
        // --- Truncate ---

        [Fact]
        public void Truncate_ReturnsEmptyString_ForNull()
        {
            string s = null;
            Assert.Equal(string.Empty, s.Truncate(10));
        }

        [Fact]
        public void Truncate_ReturnsEmptyString_ForEmptyInput()
        {
            Assert.Equal(string.Empty, "".Truncate(10));
        }

        [Fact]
        public void Truncate_ReturnsEmptyString_WhenMaxLengthIsZeroOrNegative()
        {
            // Characterizing a real edge case: a non-empty string with
            // maxLength <= 0 returns "" rather than throwing or returning
            // the original string. Current behavior, documented as-is.
            Assert.Equal(string.Empty, "hello".Truncate(0));
            Assert.Equal(string.Empty, "hello".Truncate(-1));
        }

        [Fact]
        public void Truncate_ReturnsOriginalString_WhenShorterThanMaxLength()
        {
            Assert.Equal("hello", "hello".Truncate(10));
        }

        [Fact]
        public void Truncate_ReturnsOriginalString_WhenExactlyMaxLength()
        {
            // s.Length > maxLength is the cutoff, so equal-length strings
            // are NOT truncated.
            Assert.Equal("hello", "hello".Truncate(5));
        }

        [Fact]
        public void Truncate_TruncatesAndAppendsEllipsis_WhenLongerThanMaxLength()
        {
            Assert.Equal("hel...", "hello".Truncate(3));
        }

        // --- IsNumeric ---

        [Theory]
        [InlineData("123")]
        [InlineData("123.45")]
        [InlineData("-5")]
        [InlineData("0")]
        public void IsNumeric_ReturnsTrue_ForOrdinaryNumbers(string input)
        {
            Assert.True(input.IsNumeric());
        }

        [Theory]
        [InlineData("Seattle")]
        [InlineData("")]
        [InlineData("12a")]
        public void IsNumeric_ReturnsFalse_ForNonNumericInput(string input)
        {
            Assert.False(input.IsNumeric());
        }

        [Fact]
        public void IsNumeric_ReturnsTrue_ForScientificNotation()
        {
            // Real quirk worth documenting: IsNumeric is implemented via
            // Double.TryParse with NumberStyles.Any, so "1e5" parses
            // successfully as a double (100000) and is treated as
            // "numeric" -- even though it's not a valid US zip code, which
            // is this method's actual call site (GeolocationService,
            // distinguishing a place name from a postal code). A search
            // for "1e5" would be routed to geonames.org's postalcode
            // parameter rather than placename, and would very likely
            // return no results. This is a real, if minor, behavioral
            // gap -- captured here rather than silently patched, per the
            // characterization-testing principle: document what IS true,
            // not what should be.
            Assert.True("1e5".IsNumeric());
        }

        [Fact]
        public void IsNumeric_ReturnsFalse_ForNull()
        {
            string s = null;
            Assert.False(s.IsNumeric());
        }
    }
}
