using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace NerdDinner.Proxy.Services
{
    public class LatLong
    {
        public double Lat { get; set; }
        public double Long { get; set; }
    }

    // Ported from the legacy app's Services/GeolocationService.cs, scoped
    // to just what SearchController needs (PlaceOrZipToLatLong) -- M9's
    // Search port is the only caller in this app. Given real,
    // constructor-injected configuration from the start via IConfiguration
    // (ASP.NET Core's built-in DI), rather than the legacy app's
    // ConfigurationManager.AppSettings coupling and the optional-parameter
    // stopgap that was layered on top of it purely to work around Visual
    // Studio Test Explorer (decision-log.md DL-026/DL-027). This is what
    // DL-027 flagged as the "better fix" to land here, not before.
    public class GeolocationService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public GeolocationService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public LatLong PlaceOrZipToLatLong(string placeOrZip)
        {
            var secret = Uri.EscapeDataString(_configuration["GeoNames:UserName"]);

            // See decision-log.md DL-016: api.geonames.org's HTTPS
            // certificate is issued for secure.geonames.org, not itself --
            // same non-obvious hostname requirement as the legacy app.
            string url = "https://secure.geonames.org/postalCodeSearch?{0}={1}&maxRows=1&style=SHORT&username={2}";
            url = string.Format(url, IsNumeric(placeOrZip) ? "postalcode" : "placename", placeOrZip, secret);

            try
            {
                if (!_cache.TryGetValue(placeOrZip, out XDocument result))
                {
                    result = XDocument.Load(url);
                    _cache.Set(placeOrZip, result, TimeSpan.FromDays(1));
                }

                if (result.Descendants("code").Any())
                {
                    return (from x in result.Descendants("code")
                            select new LatLong
                            {
                                Lat = (double)x.Element("lat"),
                                Long = (double)x.Element("lng")
                            }).First();
                }
                return null;
            }
            catch (Exception)
            {
                // Same "no SLA on a free third-party API" reasoning as the
                // legacy app's equivalent try/catch (DL-016) -- treat any
                // failure as "no match" rather than letting it propagate.
                return null;
            }
        }

        // Matches the legacy app's Helpers/StringExtensions.cs IsNumeric
        // exactly (Double.TryParse-based, not a digit-only check) --
        // affects which GeoNames query parameter (postalcode vs
        // placename) a given input routes to.
        private static bool IsNumeric(string value)
        {
            return double.TryParse(value, System.Globalization.NumberStyles.Any,
                System.Globalization.NumberFormatInfo.InvariantInfo, out _);
        }
    }
}
