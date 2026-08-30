using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NerdDinner.Proxy.Helpers;
using NerdDinner.Proxy.Models;

namespace NerdDinner.Proxy.Controllers
{
    // Ported from the legacy app's Controllers/DinnersController.cs (M9,
    // decision-log.md DL-028). Action bodies are kept close to verbatim --
    // same behavior, including the same pre-existing gaps (see
    // DeleteConfirmed below) -- only the MVC surface (ActionResult ->
    // IActionResult, HttpNotFound() -> NotFound(), etc.) and data access
    // (EF6 -> EF Core, constructor-injected via ASP.NET Core's built-in DI
    // rather than the legacy app's own field-initializer pattern) changed.
    public class DinnersController : Controller
    {
        private readonly NerdDinnerCoreContext db;
        private const int PageSize = 25;

        public DinnersController(NerdDinnerCoreContext context)
        {
            db = context;
        }

        // GET: /Dinners/
        public IActionResult Index(int? page)
        {
            int pageIndex = page ?? 1;

            var dinners = db.Dinners.Where(d => d.EventDate >= DateTime.Now).OrderBy(d => d.EventDate);
            return View(new PagedList<Dinner>(dinners, pageIndex, PageSize));
        }

        // GET: /Dinners/Details/5
        public IActionResult Details(int id = 0)
        {
            Dinner dinner = db.Dinners.Find(id);
            if (dinner == null)
            {
                return NotFound();
            }
            return View(dinner);
        }

        // GET: /Dinners/Create
        [Authorize]
        public IActionResult Create()
        {
            var dinner = new Dinner()
            {
                EventDate = DateTime.Now.AddDays(7),
                HostedBy = User.Identity.Name
            };

            return View(dinner);
        }

        // POST: /Dinners/Create
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public IActionResult Create(Dinner dinner)
        {
            if (ModelState.IsValid)
            {
                dinner.HostedBy = User.Identity.Name;

                RSVP rsvp = new RSVP();
                rsvp.AttendeeName = User.Identity.Name;

                dinner.RSVPs = new System.Collections.Generic.List<RSVP>();
                dinner.RSVPs.Add(rsvp);

                db.Dinners.Add(dinner);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(dinner);
        }

        // GET: /Dinners/Edit/5
        [Authorize]
        public IActionResult Edit(int id = 0)
        {
            Dinner dinner = db.Dinners.Find(id);
            if (dinner == null)
            {
                return NotFound();
            }
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }
            return View(dinner);
        }

        // POST: /Dinners/Edit/5
        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public IActionResult Edit(Dinner dinner)
        {
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }

            if (ModelState.IsValid)
            {
                db.Entry(dinner).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(dinner);
        }

        // GET: /Dinners/Delete/5
        [Authorize]
        public IActionResult Delete(int id = 0)
        {
            Dinner dinner = db.Dinners.Find(id);
            if (dinner == null)
            {
                return NotFound();
            }
            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }
            return View(dinner);
        }

        // POST: /Dinners/Delete/5
        [HttpPost, ActionName("Delete"), Authorize, ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            // Real, pre-existing bug, ported as-is rather than fixed here
            // (per DL-004): no null check on the Find() result before
            // calling IsHostedBy -- a POST to a missing id throws an
            // unhandled NRE, same as the legacy app. See
            // NerdDinner.Tests.Controllers.DinnersControllerTests
            // .DeleteConfirmed_ThrowsNullReferenceException_ForNonexistentId.
            Dinner dinner = db.Dinners.Find(id);

            if (!dinner.IsHostedBy(User.Identity.Name))
            {
                return View("InvalidOwner");
            }

            db.Dinners.Remove(dinner);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult WebSlicePopular()
        {
            ViewData["Title"] = "Popular Nerd Dinners";
            var model = from dinner in db.Dinners
                        where dinner.EventDate >= DateTime.Now
                        orderby dinner.RSVPs.Count descending
                        select dinner;
            return View("WebSlice", model.Take(5));
        }

        public IActionResult WebSliceUpcoming()
        {
            ViewData["Title"] = "Upcoming Nerd Dinners";
            DateTime d = DateTime.Now.AddMonths(2);
            var model = from dinner in db.Dinners
                        where dinner.EventDate < d
                        orderby dinner.EventDate descending
                        select dinner;
            return View("WebSlice", model.Take(5));
        }
    }
}
