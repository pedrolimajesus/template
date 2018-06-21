// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using AppComponents.ControlFlow;

namespace AppComponents.Web.Helpers
{
    /// <summary>
    ///   To use, in web.config under pages/namespaces add "AppComponents.Web.Helpers" and also put @using AppComponents.Web.Helpers at the top of the page
    /// </summary>
    public static class HtmlHelpersExtensions
    {
        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html,
                                                             Expression<Func<TModel, TValue>> expression,
                                                             object htmlAttributes)
        {
            return LabelFor(html, expression, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString LabelFor<TModel, TValue>(this HtmlHelper<TModel> html,
                                                             Expression<Func<TModel, TValue>> expression,
                                                             IDictionary<string, object> htmlAttributes)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);

            var htmlFieldName = ExpressionHelper.GetExpressionText(expression);

            var labelText = metadata.DisplayName ?? metadata.PropertyName ?? htmlFieldName.Split('.').Last();

            if (String.IsNullOrEmpty(labelText))
            {
                return MvcHtmlString.Empty;
            }


            var tag = new TagBuilder("label");

            tag.MergeAttributes(htmlAttributes);

            tag.Attributes.Add("for", html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(htmlFieldName));

            tag.SetInnerText(labelText);

            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }


        public static MvcHtmlString Lookup<TModel>(this HtmlHelper<TModel> html, string id)
        {
            return MvcHtmlString.Create(ContextualString.Get(id));
        }

        public static MvcHtmlString Format<TModel>(this HtmlHelper<TModel> html, string id, params object[] args)
        {
            return MvcHtmlString.Create(ContextualString.Merge(id, args));
        }

        public static MvcHtmlString LookupImage<TModel, TValue>(this HtmlHelper<TModel> html, string id,
                                                                Expression<Func<TModel, TValue>> expression,
                                                                object htmlAttributes)
        {
            return LookupImage(html, id, expression, new RouteValueDictionary(htmlAttributes));
        }

        public static MvcHtmlString LookupImage<TModel, TValue>(this HtmlHelper<TModel> html, string id,
                                                                Expression<Func<TModel, TValue>> expression,
                                                                IDictionary<string, object> htmlAttributes)
        {
            var tag = new TagBuilder("img");
            tag.MergeAttributes(htmlAttributes);
            tag.Attributes.Add("src", ContextualString.Get(id));
            return MvcHtmlString.Create(tag.ToString(TagRenderMode.Normal));
        }

        public static MvcHtmlString ContextualBundle(string key)
        {
            return null;
            //return MvcHtmlString.Create(BundleSpec.MakeContextualSpecKey(key));
        }

        public static bool IsCurrentAction(this HtmlHelper helper, string actionName, string controllerName)
        {
            var currentControllerName = (string)helper.ViewContext.RouteData.Values["controller"];
            var currentActionName = (string)helper.ViewContext.RouteData.Values["action"];

            return currentControllerName.Equals(controllerName, StringComparison.CurrentCultureIgnoreCase) &&
                   currentActionName.Equals(actionName, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}