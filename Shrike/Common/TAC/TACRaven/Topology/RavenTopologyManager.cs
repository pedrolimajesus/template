using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Raven;
using Raven.Client.Linq;

namespace AppComponents.Topology
{
    public class RavenTopologyManager: IApplicationTopologyManager
    {
        public RavenTopologyManager()
        {
            
        }

        public IEnumerable<ApplicationNode> GetTopology()
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var qry = (IRavenQueryable<ApplicationNode>) dc.Query<ApplicationNode>();
                    var all = qry.GetAllUnSafe();
                    return all;
                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());

            }

            return null;
        }

        public void ResetDefaultLoggingConfiguration(string applicationNodeId)
        {

            
            ConfigureLogging(applicationNodeId, string.Empty, 0, null);
        }

        public void ConfigureLogging(string applicationNodeId, string filter = null, int level = -1, string file = null)
        {
            var loggingConfig = new LoggingConfiguration
                {
                    ClassFilter = filter,
                    File = file,
                    LogLevel = level
                };

            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var item = dc.Load<ApplicationNode>(applicationNodeId);
                    if (null != item)
                    {
                        item.LoggingConfiguration = loggingConfig;
                        dc.SaveChanges();
                    }
                       
                }
            }
            catch (Exception ex)
            {

                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
        }

        public void SetAlertHandled(string applicationNodeId, string alertId)
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var item = dc.Load<ApplicationNode>(applicationNodeId);
                    
                    if (null != item)
                    {
                        
                        var alertItem = item.Alerts.FirstOrDefault(it => it.Id == alertId);
                        if (null != alertItem)
                        {
                            item.Alerts.Remove(alertItem);
                            dc.Advanced.UseOptimisticConcurrency = false;

                            dc.SaveChanges();
                        }

                    }

                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());   
            }
        }

        public void PauseNode(string applicationNodeId)
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var item = dc.Load<ApplicationNode>(applicationNodeId);
                    if (null != item)
                    {
                        item.State = ApplicationNodeStates.Paused;
                        dc.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
        }

        public void RunNode(string applicationNodeId)
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var item = dc.Load<ApplicationNode>(applicationNodeId);
                    if (null != item)
                    {
                        item.State = ApplicationNodeStates.Running;
                        dc.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
        }

        public void DeleteNodeInformation(string applicationNodeId)
        {
            try
            {
                using (var dc = DocumentStoreLocator.Resolve(DocumentStoreLocator.RootLocation))
                {
                    var item = dc.Load<ApplicationNode>(applicationNodeId);
                    if (null != item)
                    {
                        dc.Delete(item);
                        dc.SaveChanges();
                    }

                }
            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());
            }
        }
    }
}
