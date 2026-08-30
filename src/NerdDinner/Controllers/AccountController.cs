using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NerdDinner.Models;

namespace NerdDinner.Controllers
{
    // Ported from the legacy app's Controllers/AccountController.cs (M10,
    // decision-log.md DL-029). ASP.NET Core Identity's UserManager/
    // SignInManager replace ASP.NET Identity 2.x's OWIN-based equivalents
    // -- same core concepts (both are the same Microsoft Identity lineage),
    // different package and DI-native construction (constructor injection
    // instead of HttpContext.GetOwinContext()).
    //
    // Local username/password login, registration, and logoff are the
    // fully live-tested path here (see AccountControllerTests /
    // AccountControllerIdentityTests). The external-provider flows
    // (ExternalLogin/ExternalLoginCallback/Disassociate) are ported with
    // the same structural fidelity as the legacy app's, but stay
    // untested at the live level for the same reason the legacy suite
    // never tested them either: no OAuth provider secrets are configured
    // in this dev environment (all four ship blank in appsettings.json,
    // same externalized-but-unconfigured pattern as the legacy Web.config).
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToLocal(returnUrl);
                }
            }

            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }

        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.UserName };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                AddErrors(result);
            }

            return View(model);
        }

        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disassociate(string provider, string providerUserId)
        {
            ManageMessageId? message = null;
            var user = await _userManager.GetUserAsync(User);

            var result = await _userManager.RemoveLoginAsync(user, provider, providerUserId);
            if (result.Succeeded)
            {
                if (user != null)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        // GET: /Account/Manage
        public async Task<IActionResult> Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            var user = await _userManager.GetUserAsync(User);
            ViewBag.HasLocalPassword = await _userManager.HasPasswordAsync(user);
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(LocalPasswordModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            bool hasLocalAccount = await _userManager.HasPasswordAsync(user);
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                var state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", string.Format("Unable to create local account. An account with the name \"{0}\" may already exist.", User.Identity.Name));
                    }
                }
            }

            return View(model);
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(loginInfo.LoginProvider, loginInfo.ProviderKey, isPersistent: false);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                await _userManager.AddLoginAsync(user, loginInfo);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                ViewBag.ProviderDisplayName = loginInfo.ProviderDisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel
                {
                    UserName = loginInfo.Principal.FindFirstValue(ClaimTypes.Name) ?? loginInfo.Principal.FindFirstValue(ClaimTypes.Email),
                    ExternalLoginData = SerializeExternalLoginData(loginInfo)
                });
            }
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated || !TryDeserializeExternalLoginData(model.ExternalLoginData, out var provider, out var providerKey))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                var loginInfo = await _signInManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
                    return View("ExternalLoginFailure");
                }

                var user = new ApplicationUser { UserName = model.UserName };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, loginInfo);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ProviderDisplayName = provider;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public IActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers

        private static string SerializeExternalLoginData(ExternalLoginInfo loginInfo)
        {
            return loginInfo.LoginProvider + "|" + loginInfo.ProviderKey;
        }

        private static bool TryDeserializeExternalLoginData(string data, out string provider, out string providerKey)
        {
            provider = null;
            providerKey = null;
            if (string.IsNullOrEmpty(data))
            {
                return false;
            }

            int separatorIndex = data.IndexOf('|');
            if (separatorIndex < 0)
            {
                return false;
            }

            provider = data.Substring(0, separatorIndex);
            providerKey = data.Substring(separatorIndex + 1);
            return true;
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        #endregion
    }
}
