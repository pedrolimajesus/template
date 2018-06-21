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
#if DONOT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Optimization;
using AppComponents.ControlFlow;
using AppComponents.Extensions.EnumEx;
using Newtonsoft.Json;

namespace AppComponents.Web
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class BundleSpecificationAttribute : Attribute
    {
        public BundleSpecificationAttribute(string resourceName)
        {
            ResourceName = resourceName;
        }

        public string ResourceName { get; private set; }
    }

    public class BundleSpec
    {
        public string ApplicationType { get; set; }
        public string Culture { get; set; }
        public string Key { get; set; }
        public List<string> FilePaths { get; set; }

        public string SpecKey
        {
            get { return MakeSpecKey(Key, Culture, ApplicationType); }
        }

        public static string MakeSpecKey(string key, string culture, string appType)
        {
            return string.Format("{0}//{1}//{2}", key, culture, appType);
        }

        public static string MakeContextualSpecKey(string key)
        {
            var appCtx = ContextRegistry.ContextsOf("ApplicationType").First();
            var cultureCtx = ContextRegistry.ContextsOf("Culture").First();
            return MakeSpecKey(key, appCtx.Segments.First(), cultureCtx.Segments.First());
        }
    }

    public class BundleManfest
    {
        public List<BundleSpec> JsBundles { get; set; }
        public List<BundleSpec> CssBundles { get; set; }
    }


    public static class BundleManager
    {
        public static void Initialize()
        {
            var bs = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                ass => ass.GetCustomAttributes(typeof (BundleSpecificationAttribute), true)).Select(
                    o => (BundleSpecificationAttribute) o).FirstOrDefault();
            if (null != bs)
            {
                ResourceInitialize(bs.ResourceName);
            }
        }

        public static void Initialize(Enum resourceManifest)
        {
            ResourceInitialize(resourceManifest.EnumName());
        }

        private static void ResourceInitialize(string resourceName)
        {
            var src = new StringResourcesCache();
            var manifest = src[resourceName + ".json"];
            Initialize(manifest);
        }

        public static void Initialize(string jsonManifest)
        {
            Initialize(JsonConvert.DeserializeObject<BundleManfest>(jsonManifest));
        }

        public static void Initialize(BundleManfest manifest)
        {
            IBundleTransform jstransformer;
            IBundleTransform csstransformer;

#if DEBUG
            jstransformer = new PassThroughTransform("text/javascript");
            csstransformer = new PassThroughTransform("text/css");
#else
            jstransformer = new JsMinify();
            csstransformer = new CssMinify();
#endif

            foreach (var bs in manifest.JsBundles)
            {
                Bundle jsBundle = new Bundle(bs.SpecKey, jstransformer);

                jsBundle.Include(bs.FilePaths.ToArray());

                BundleTable.Bundles.Add(jsBundle);
            }

            foreach (var bs in manifest.CssBundles)
            {
                Bundle cssBundle = new Bundle(bs.SpecKey, csstransformer);

                cssBundle.Include(bs.FilePaths.ToArray());

                BundleTable.Bundles.Add(cssBundle);
            }
        }

        #region Nested type: PassThroughTransform

        public class PassThroughTransform : IBundleTransform
        {
            internal static readonly PassThroughTransform Instance;

            static PassThroughTransform()
            {
                Instance = new PassThroughTransform();
            }

            public PassThroughTransform()
            {
            }

            public PassThroughTransform(string contentType)
            {
                ContentType = contentType;
            }

            public string ContentType { get; set; }

            #region IBundleTransform Members

            public virtual void Process(BundleContext context, BundleResponse response)
            {
                bool files;
                bool flag = response != null;
                if (flag)
                {
                    flag = string.IsNullOrEmpty(ContentType);
                    if (flag)
                    {
                        if (!string.IsNullOrEmpty(response.ContentType))
                        {
                            files = true;
                        }
                        else
                        {
                            files = response.Files == null;
                        }
                        flag = files;
                        if (!flag)
                        {
                            FileInfo fileInfo = response.Files.FirstOrDefault();
                            flag = fileInfo == null;
                            if (!flag)
                            {
                                flag = !string.Equals(fileInfo.Extension, ".js", StringComparison.OrdinalIgnoreCase);
                                if (flag)
                                {
                                    flag =
                                        !string.Equals(fileInfo.Extension, ".css", StringComparison.OrdinalIgnoreCase);
                                    if (!flag)
                                    {
                                        response.ContentType = "text/css";
                                    }
                                }
                                else
                                {
                                    response.ContentType = "text/javascript";
                                }
                            }
                        }
                    }
                    else
                    {
                        response.ContentType = ContentType;
                    }
                    return;
                }
                else
                {
                    throw new ArgumentNullException("response");
                }
            }

            #endregion
        }

        #endregion
    }
}

#endif