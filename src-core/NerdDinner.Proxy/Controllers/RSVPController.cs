using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NerdDinner.Proxy.Models;

namespace NerdDinner.Proxy.Controllers
{
    // Ported from the legacy app's Controllers/RSVPController.cs (M9,
    // decision-log.md DL-028) -- same behavior, including the
    // characterized RegisterForDinner NRE gap on a nonexistent dinner id.
    public class RSVPController : Controller
    {
        private readonly NerdDinnerCoreContext db;

        public RSVPController(NerdDinnerCoreContext context)
        {
            db = context;
        }

        // HTTP: /RSVP/Register/1
        [Authorize]
        public IActionResult Register(int id)
        {
            RegisterForDinner(id);
            return RedirectToAction("Details", "Dinners", new { id = id });
        }

        // AJAX: /Dinners/RegisterAjax/1
        [Authorize, HttpPost]
        public IActionResult RegisterAjax(int id)
        {
            RegisterForDinner(id);
            return Content("Thanks - we'll see you there!");
        }

        private void RegisterForDinner(int id)
        {
            Dinner dinner = db.Dinners.Find(id);

            if (!dinner.IsUserRegistered(User.Identity.Name))
            {
                RSVP rsvp = new RSVP();
                rsvp.AttendeeName = User.Identity.Name;

                dinner.RSVPs.Add(rsvp);
                db.SaveChanges();
            }
        }

        // AJAX: /RSVP/CancelAjax/1
        [Authorize, HttpPost]
        public IActionResult CancelAjax(int id)
        {
            Dinner dinner = db.Dinners.Find(id);

            RSVP rsvp = dinner.RSVPs.SingleOrDefault(r => this.User.Identity.Name == r.AttendeeName);
            if (rsvp != null)
            {
                db.RSVPs.Remove(rsvp);
                db.SaveChanges();
            }

            return Content("Sorry you can't make it!");
        }
    }
}
