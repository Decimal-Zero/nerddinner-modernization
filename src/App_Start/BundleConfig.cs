using System.Web;
using System.Web.Optimization;

namespace NerdDinner
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.unobtrusive*",
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/knockout").Include(
                        "~/Scripts/knockout-*"));

            bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));

            // jQuery UI 1.14.1 ships one combined theme stylesheet instead of
            // the per-component files (jquery.ui.core.css, etc.) that the
            // 1.10.2 package used -- this bundle covers the same components.
            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                        "~/Content/themes/base/jquery-ui.css"));
        }
    }
}