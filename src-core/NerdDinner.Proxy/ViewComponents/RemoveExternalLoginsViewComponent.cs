using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NerdDinner.Proxy.Models;

namespace NerdDinner.Proxy.ViewComponents
{
    // Replaces the legacy app's AccountController.RemoveExternalLogins
    // [ChildActionOnly] action. Unlike the legacy version (synchronous
    // UserManager calls only, since classic MVC child actions invoked via
    // @Html.Action couldn't be async), View Components support
    // InvokeAsync directly.
    public class RemoveExternalLoginsViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RemoveExternalLoginsViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            var linkedAccounts = await _userManager.GetLoginsAsync(user);

            var externalLogins = new List<ExternalLogin>();
            foreach (var account in linkedAccounts)
            {
                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.LoginProvider,
                    ProviderDisplayName = account.ProviderDisplayName ?? account.LoginProvider,
                    ProviderUserId = account.ProviderKey,
                });
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);
            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || hasPassword;
            return View(externalLogins);
        }
    }
}
