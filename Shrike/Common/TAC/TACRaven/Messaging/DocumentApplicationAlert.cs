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
using AppComponents.Raven;
//using Newtonsoft.Json;
using log4net;

namespace AppComponents
{
    using global::Raven.Imports.Newtonsoft.Json;


    public enum DocumentApplicationAlertLocalConfig
    {
        ComponentOrigin
    }

    public class AlertsLog
    {
        public AlertsLog()
        {
            EventTime = DateTime.UtcNow;
        }

        public string Id { get; set; }
        public DateTime EventTime { get; set; }
        public ApplicationAlertKind Kind { get; set; }
        public string Detail { get; set; }
        public string MachineOrigin { get; set; }
        public string ComponentOrigin { get; set; }
        public bool Handled { get; set; }
    }

    public class DocumentApplicationAlert : IApplicationAlert
    {
        public string _componentOrigin;

        public DocumentApplicationAlert()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            if (cf.SettingExists(DocumentApplicationAlertLocalConfig.ComponentOrigin))
            {
                var componentType = cf.Get<Type>(DocumentApplicationAlertLocalConfig.ComponentOrigin);
                _componentOrigin = componentType.FullName;
            }
        }

        #region IApplicationAlert Members

        public void RaiseAlert(ApplicationAlertKind kind, params object[] details)
        {
            try
            {
                
                var jsonDetail = JsonConvert.SerializeObject(details);
                var ol = new AlertsLog
                             {
                                 Kind = kind,
                                 Detail = jsonDetail,
                                 MachineOrigin = Environment.MachineName,
                                 ComponentOrigin = _componentOrigin ?? "Unknown"
                             };

                var strEv = string.Format("Operational Event {0}:\n{1}", ol.Kind, ol.Detail);

                // record to table.
                try
                {
                    using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                    {
                        ds.Store(ol);
                        ds.SaveChanges();
                    }
                }
                catch
                {
                }

                // log it.
                try
                {
                    var log = LogManager.GetLogger(GetType());
                    log.Fatal(strEv);
                }
                catch
                {
                }

               
            }
            catch // could be in adverse conditions. 
            {
            }
        }

        #endregion
    }
}