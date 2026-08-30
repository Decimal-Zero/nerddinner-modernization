using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NerdDinner.Models;

namespace NerdDinner.ViewComponents
{
    // Replaces the legacy app's AccountController.ExternalLoginsList
    // [ChildActionOnly] action -- ASP.NET Core MVC has no equivalent to
    // classic MVC's Html.Action/child actions; View Components are the
    // idiomatic replacement.
    public class ExternalLoginsListViewComponent : ViewComponent
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ExternalLoginsListViewComponent(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
            return View(schemes);
        }
    }
}
