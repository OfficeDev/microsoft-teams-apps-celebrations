// <copyright file="BundleConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.Celebration
{
    using System.Web.Optimization;

    /// <summary>
    /// Configures bundles
    /// </summary>
    public class BundleConfig
    {
        /// <summary>
        /// Configures bundles.
        /// </summary>
        /// <param name="bundles">Bundle collection to configure</param>
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/lib/jquery/jquery.js"));
            bundles.Add(new ScriptBundle("~/bundles/jquery-validate").Include(
                "~/lib/jquery-validate/jquery.validate.js"));
            bundles.Add(new ScriptBundle("~/bundles/jquery-slimscroll").Include(
                "~/lib/jquery-slimscroll/jquery.slimscroll.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap3").Include(
                "~/lib/bootstrap3/dist/js/bootstrap.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap4").Include(
                "~/lib/bootstrap4/js/bootstrap.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap-multiselect").Include(
                "~/lib/bootstrap-multiselect/js/bootstrap-multiselect.js"));
            bundles.Add(new ScriptBundle("~/bundles/bootstrap-datepicker").Include(
                "~/Scripts/bootstrap-datepicker.js"));

            bundles.Add(new ScriptBundle("~/bundles/momentjs").Include(
                "~/lib/moment.js/moment.js"));
            bundles.Add(new ScriptBundle("~/bundles/adal-js").Include(
                "~/lib/adal-angular/dist/adal.min.js"));
            bundles.Add(new ScriptBundle("~/bundles/teams-js").Include(
                "~/lib/teams-js/dist/microsoftTeams.min.js"));

            bundles.Add(new StyleBundle("~/bundles/css").Include(
                "~/Content/css/styles.css",
                "~/Content/css/tabStyles.css",
                "~/Content/css/button-themes.css",
                "~/Content/css/spinner.css"));

            bundles.Add(new StyleBundle("~/bundles/bootstrap3-css").Include(
                "~/lib/bootstrap3/dist/css/bootstrap.css"));
            bundles.Add(new StyleBundle("~/bundles/bootstrap4-css").Include(
                "~/lib/bootstrap4/css/bootstrap.css"));
            bundles.Add(new StyleBundle("~/bundles/bootstrap-multiselect-css").Include(
                "~/lib/bootstrap-multiselect/css/bootstrap-multiselect.css"));
            bundles.Add(new StyleBundle("~/bundles/bootstrap-datepicker-css").Include(
                "~/Content/css/datepicker.css",
                "~/Content/css/datepicker-custom-theme.css"));
            bundles.Add(new StyleBundle("~/content/fontawesome/css/font-awesome").Include(
                "~/Content/fontawesome/css/font-awesome.css"));
        }
    }
}
