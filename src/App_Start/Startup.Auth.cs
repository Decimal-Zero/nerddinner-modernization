using System.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.MicrosoftAccount;
using Microsoft.Owin.Security.Twitter;
using NerdDinner.Models;
using Owin;

namespace NerdDinner
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // External login providers: keys stay externalized to config,
            // same as the DotNetOpenAuth-based setup this replaces (see
            // decision-log.md DL-014). Google is the one behavior change --
            // it now requires a registered client id/secret pair, where the
            // old OpenID 2.0-based flow didn't need any configuration at
            // all (Google retired OpenID 2.0 in 2015, so that flow was
            // already non-functional in practice).

            var microsoftClientId = ConfigurationManager.AppSettings["microsoftClientId"];
            var microsoftClientSecret = ConfigurationManager.AppSettings["microsoftClientSecret"];
            if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
            {
                app.UseMicrosoftAccountAuthentication(
                    clientId: microsoftClientId,
                    clientSecret: microsoftClientSecret);
            }

            var twitterConsumerKey = ConfigurationManager.AppSettings["twitterConsumerKey"];
            var twitterConsumerSecret = ConfigurationManager.AppSettings["twitterConsumerSecret"];
            if (!string.IsNullOrEmpty(twitterConsumerKey) && !string.IsNullOrEmpty(twitterConsumerSecret))
            {
                app.UseTwitterAuthentication(
                    consumerKey: twitterConsumerKey,
                    consumerSecret: twitterConsumerSecret);
            }

            var facebookAppId = ConfigurationManager.AppSettings["facebookAppId"];
            var facebookAppSecret = ConfigurationManager.AppSettings["facebookAppSecret"];
            if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
            {
                app.UseFacebookAuthentication(
                    appId: facebookAppId,
                    appSecret: facebookAppSecret);
            }

            var googleClientId = ConfigurationManager.AppSettings["googleClientId"];
            var googleClientSecret = ConfigurationManager.AppSettings["googleClientSecret"];
            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret,
                });
            }
        }
    }
}
