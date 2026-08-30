using System;
using System.Collections.Specialized;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.Web.Mvc;
using Xunit;

namespace NerdDinner.Tests.ModelBinders
{
    /// <summary>
    /// Found by manual testing (creating a dinner without a geocoded
    /// location), not by the automated suite -- same pattern as the
    /// ws.geonames.org retirement and the GeoNames username requirement
    /// (see m2-characterization-tests.md, decision-log.md DL-013): a real
    /// gap only surfaces once something actually exercises the code.
    /// </summary>
    public class DbGeographyModelBinderTests
    {
        static DbGeographyModelBinderTests()
        {
            // DbGeography.FromText needs the native SqlServerSpatial DLL
            // (see decision-log.md DL-010) -- normally loaded by
            // TestDatabaseFixture, which this class doesn't share a
            // collection with, so it isn't guaranteed to have run first
            // when these tests are executed in isolation.
            //
            // Assembly.CodeBase, not AppDomain.CurrentDomain.BaseDirectory
            // or Assembly.Location (both unreliable under the VSTest
            // adapter/IDE Test Explorer) -- see the identical
            // fix/explanation in TestDatabase.cs and decision-log.md
            // DL-023.
            var codeBaseUri = new Uri(typeof(DbGeographyModelBinderTests).Assembly.CodeBase);
            var testAssemblyDirectory = System.IO.Path.GetDirectoryName(codeBaseUri.LocalPath);
            SqlServerTypes.Utilities.LoadNativeAssemblies(testAssemblyDirectory);
        }

        private static object BindLocation(string postedValue)
        {
            var values = new NameValueCollection();
            if (postedValue != null)
            {
                values.Add("Location", postedValue);
            }

            var bindingContext = new ModelBindingContext
            {
                ModelName = "Location",
                ValueProvider = new NameValueCollectionValueProvider(values, CultureInfo.InvariantCulture),
            };

            var binder = new NerdDinner.DbGeographyModelBinder();
            return binder.BindModel(null, bindingContext);
        }

        [Fact]
        public void BindModel_ReturnsNull_WhenPostedValueIsEmpty()
        {
            // The real-world trigger: DbGeography.cshtml posts "" for
            // Location whenever nothing has been geocoded yet -- this
            // used to throw IndexOutOfRangeException instead of
            // returning null.
            var result = BindLocation("");

            Assert.Null(result);
        }

        [Fact]
        public void BindModel_ReturnsNull_WhenFieldIsNotPosted()
        {
            var result = BindLocation(null);

            Assert.Null(result);
        }

        [Fact]
        public void BindModel_ReturnsNull_ForMalformedValue_WithNoComma()
        {
            var result = BindLocation("notalatlongpair");

            Assert.Null(result);
        }

        [Fact]
        public void BindModel_ReturnsDbGeography_ForWellFormedLatLongPair()
        {
            var result = BindLocation("47.608013,-122.335167") as DbGeography;

            Assert.NotNull(result);
            Assert.Equal(47.608013, result.Latitude.Value, 6);
            Assert.Equal(-122.335167, result.Longitude.Value, 6);
        }
    }
}
