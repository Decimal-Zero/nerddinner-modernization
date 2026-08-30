using System;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Xml.Linq;
using NerdDinner.Helpers;

namespace NerdDinner.Services
{
    public class GeolocationService
    {
        // geoNamesUserName: lets a caller supply the value directly instead
        // of relying on ConfigurationManager.AppSettings, which -- like the
        // connection-string lookups in NerdDinnerContext/ApplicationDbContext
        // (decision-log.md DL-024/DL-025) -- doesn't reliably resolve this
        // assembly's own config under Visual Studio's IDE-hosted Test
        // Explorer. Defaults to null, in which case behavior is unchanged:
        // falls back to ConfigurationManager.AppSettings, same as always.
        // The app's own call site (SearchController) never passes this.
        public static LatLong PlaceOrZipToLatLong(string placeOrZip, string geoNamesUserName = null)
        {
            ObjectCache cache = MemoryCache.Default;

            var secret = Uri.EscapeDataString(geoNamesUserName ?? ConfigurationManager.AppSettings["GeoNames:UserName"]);

            // HTTPS on this API is served from a different hostname than
            // HTTP -- api.geonames.org's TLS certificate is issued for
            // secure.geonames.org, so switching only the scheme on the
            // same host fails with a certificate/SNI mismatch. Confirmed
            // directly rather than assumed (see decision-log.md DL-016).
            string url = "https://secure.geonames.org/postalCodeSearch?{0}={1}&maxRows=1&style=SHORT&username={2}";
            url = String.Format(url, placeOrZip.IsNumeric() ? "postalcode" : "placename", placeOrZip, secret);

            try
            {
                var result = cache[placeOrZip] as XDocument;
                if (result == null)
                {
                    result = XDocument.Load(url);
                    cache.Add(placeOrZip, result,
                        new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromDays(1) });
                }

                if (result.Descendants("code").Any())
                {
                    var ll = (from x in result.Descendants("code")
                              select new LatLong
                              {
                                  Lat = (float)x.Element("lat"),
                                  Long = (float)x.Element("lng")
                              })
                               .First();
                    return ll;
                }
                return null;
            }
            catch (Exception)
            {
                // The free-tier geocoding API this depends on has no SLA --
                // rate limits, outages, or a malformed/unexpected response
                // are all real possibilities (this is the exact risk the
                // assessment's Category 7 finding called out). Treat any
                // failure here the same as "no match found" rather than
                // letting it take down the caller.
                return null;
            }
        }

        // ipInfoDbKey: same test-only escape hatch as geoNamesUserName
        // above. Defaults to null (falls back to ConfigurationManager, same
        // as always); the app itself never passes this.
        public static LocationInfo HostIpToPlaceName(string ip, string ipInfoDbKey = null)
        {
            string apiKey = ipInfoDbKey ?? ConfigurationManager.AppSettings["ipInfoDbKey"];
            string url = "https://api.ipinfodb.com/v3/ip-city/?ip={0}&key=" + apiKey;
            url = String.Format(url, ip);

            try
            {
                var result = XDocument.Load(url);

                return (from x in result.Descendants("Response")
                        select new LocationInfo
                        {
                            City = (string)x.Element("City"),
                            RegionName = (string)x.Element("RegionName"),
                            Country = (string)x.Element("CountryName"),
                            ZipPostalCode = (string)x.Element("CountryName"),
                            Position = new LatLong
                            {
                                Lat = (float)x.Element("Latitude"),
                                Long = (float)x.Element("Longitude")
                            }
                        }).FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class LatLong
    {
        public float Lat { get; set; }
        public float Long { get; set; }
    }

    public class LocationInfo
    {
        public string Country { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
        public string ZipPostalCode { get; set; }
        public LatLong Position { get; set; }
    }
}