using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NerdDinner.Proxy.Models;
using NerdDinner.Proxy.Services;

namespace NerdDinner.Proxy.Controllers
{
    public class JsonDinner
    {
        public int DinnerID { get; set; }
        public DateTime EventDate { get; set; }
        public string Title { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Description { get; set; }
        public int RSVPCount { get; set; }
        public string Url { get; set; }
    }

    // Ported from the legacy app's Controllers/SearchController.cs (M9,
    // decision-log.md DL-028) -- moved into NerdDinner.Proxy per M8/DL-021's
    // scope correction. NerdDinner.js's calls (GET api/Search?location=,
    // POST api/Search?limit=) are unchanged, so no client-side changes
    // were needed.
    //
    // Route shape difference from the legacy ApiController: classic Web
    // API's action selector disambiguated SearchByLocation (GET,
    // latitude/longitude) from SearchByPlaceNameOrZip (GET, location) on
    // the same "api/Search" route purely by which query-string parameters
    // were present -- a convention ASP.NET Core's router doesn't
    // replicate for two actions on one route/verb pair. Folded into a
    // single Get() that does the same dispatch explicitly instead of
    // relying on framework magic that no longer exists; externally, the
    // URL contract is identical.
    [Route("api/Search")]
    public class SearchController : Controller
    {
        private readonly NerdDinnerCoreContext db;
        private readonly GeolocationService geolocationService;

        public SearchController(NerdDinnerCoreContext context, GeolocationService geolocationService)
        {
            db = context;
            this.geolocationService = geolocationService;
        }

        // GET api/Search?latitude=1.0&longitude=1.0
        // GET api/Search?location=30901
        // GET api/Search?location=Seattle
        [HttpGet]
        public IActionResult Get(double? latitude, double? longitude, string location)
        {
            if (latitude.HasValue && longitude.HasValue)
            {
                return Json(SearchByLocation(latitude.Value, longitude.Value));
            }

            return Json(SearchByPlaceNameOrZip(location));
        }

        public IEnumerable<JsonDinner> SearchByLocation(double latitude, double longitude)
        {
            return FindByLocation(latitude, longitude);
        }

        public IEnumerable<JsonDinner> SearchByPlaceNameOrZip(string location)
        {
            if (string.IsNullOrEmpty(location)) return null;
            LatLong foundlocation = geolocationService.PlaceOrZipToLatLong(location);
            if (foundlocation != null)
            {
                return FindByLocation(foundlocation.Lat, foundlocation.Long)
                                .OrderByDescending(p => p.EventDate);
            }
            return null;
        }

        // POST api/Search?limit=10
        [HttpPost]
        public IEnumerable<JsonDinner> GetMostPopularDinners(int limit)
        {
            var mostPopularDinners = from dinner in db.Dinners.Include(d => d.RSVPs)
                                      where dinner.EventDate >= DateTime.Now
                                      orderby dinner.RSVPs.Count descending
                                      select dinner;

            return mostPopularDinners.Take(limit).AsEnumerable().Select(item => JsonDinnerFromDinner(item));
        }

        protected IQueryable<JsonDinner> FindByLocation(double latitude, double longitude)
        {
            var sourcePoint = new Point(longitude, latitude) { SRID = 4326 };

            var results =
                db.Dinners
                .Where(loc => loc.Location.Distance(sourcePoint) < 2000)
                .OrderBy(loc => loc.Location.Distance(sourcePoint));

            foreach (Dinner dinner in results)
            {
                dinner.RSVPs = new List<RSVP>();

                var rsvps = db.RSVPs.Where(x => x.DinnerID == dinner.DinnerID);

                foreach (RSVP rsvp in rsvps)
                {
                    dinner.RSVPs.Add(rsvp);
                }
            }

            var jsonDinners = results.AsEnumerable()
                    .Select(item => JsonDinnerFromDinner(item));

            return jsonDinners.AsQueryable();
        }

        private JsonDinner JsonDinnerFromDinner(Dinner dinner)
        {
            return new JsonDinner
            {
                DinnerID = dinner.DinnerID,
                EventDate = dinner.EventDate,
                Latitude = dinner.Location.Y,
                Longitude = dinner.Location.X,
                Title = dinner.Title,
                Description = dinner.Description,
                RSVPCount = dinner.RSVPs.Count(),
                Url = dinner.DinnerID.ToString()
            };
        }
    }
}
