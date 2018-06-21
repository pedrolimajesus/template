using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;

namespace Shrike.App_Start
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/Theme/Default/Site")
                .Include("~/Content/Theme/Default/Site.css")
                );

            bundles.Add(new StyleBundle("~/Content/Theme/Default/styles/jquery")
                .Include("~/Content/Theme/Default/styles/jquery.jscrollpane.css")
                .Include("~/Content/Theme/Default/styles/jquery.multiselect.css")
                .Include("~/Content/Theme/Default/styles/jquery.multiselect.filter.css")
                );

            bundles.Add(
                new StyleBundle("~/Content/Theme/Default/styles/jquery-ui").Include(
                    "~/Content/Theme/Default/styles/jquery-ui-1.9.2.custom.css"));

            //jquery bundle
            bundles.Add(new ScriptBundle("~/bundles/jquery")
                .Include("~/Scripts/jquery-{version}.js"));

            //jqueryvalidation
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.unobtrusive*",
                "~/Scripts/jquery.validate*",
                "~/Scripts/jquery.dynamic.validate.unobtrusive.js"));

            //scripts for managing tags, taggable entities
            bundles.Add(new ScriptBundle("~/bundles/customtags")
                .Include("~/Scripts/jquery.jscrollpane.js")
                .Include("~/Scripts/jquery.jscrollpane.min.js")
                .Include("~/Scripts/jquery.multiselect.js")
                .Include("~/Scripts/jquery.multiselect.filter.js")
                .Include("~/Scripts/jquery.carouFredSel-6.2.0-packed.js"));

            //jqueryui
            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                "~/Scripts/jquery-ui-{version}.js", "~/Scripts/jquery-ui.unobtrusive-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/customs")
                .Include("~/Scripts/shrike-functions.js")
                .Include("~/Scripts/shrike.loading.js"));

            bundles.Add(new ScriptBundle("~/bundles/Tag")
                .Include("~/Scripts/jquery.carouFredSel-6.2.0-packed.js")
                .Include("~/Scripts/jquery.multiselect.filter.js")
                .Include("~/Scripts/jquery.multiselect.js")
                .Include("~/Scripts/jquery.calculation.js"));

            //timezone
            bundles.Add(new ScriptBundle("~/bundles/timezone").Include("~/Scripts/jstz.js"));
        }
    }
}