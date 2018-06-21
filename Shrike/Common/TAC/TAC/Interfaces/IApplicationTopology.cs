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
using System.Net;
using System.Reflection.Emit;
using AppComponents.Data;
using Raven.Imports.Newtonsoft.Json;
using log4net;


namespace AppComponents
{
    /// <summary>
    /// 
    /// </summary>
    public enum ApplicationNodeStates
    {
        Running,
        Paused,
        Stopped,
        NotResponding
    }

    public enum GlobalConfigItemEnum
    {
        DistributedFileShare,
        DefaultDataConnection,
        DefaultDataUser,
        DefaultDataDatabase,
        DefaultDataPassword,
        EmailServer,
        EmailAccount,
        EmailPassword,
        EmailPort,
        EmailReplyAddress,
        UseSSL
    }

    /// <summary>
    /// 
    /// </summary>
    public class GlobalConfigItem
    {
        [DocumentIdentifier]
        public string Name { get; set; }

        public string Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NodeMetric
    {
        public string Category { get; set; }
        public string MetricName { get; set; }
        public string DisplayCategory { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class NodeAlert
    {
        public NodeAlert()
        {
            EventTime = DateTime.UtcNow;
        }

        [DocumentIdentifier]
        public string Id { get; set; }

        public string Title { get; set; }
        public DateTime EventTime { get; set; }
        public ApplicationAlertKind Kind { get; set; }
        public string Detail { get; set; }
        public string ComponentOrigin { get; set; }
        public bool Handled { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class LoggingConfiguration
    {
        public string DefaultFile { get; set; }
        public string File { get; set; }
        public string ClassFilter { get; set; }
        public int LogLevel { get; set; } 
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApplicationNode
    {
        public ApplicationNode()
        {
            HotfixNames = new List<string>();
            Metrics = new List<NodeMetric>();
            Alerts = new List<NodeAlert>();
            LoggingConfiguration = new LoggingConfiguration();
            Id = Guid.NewGuid().ToString();
        }

        [DocumentIdentifier] 
        public string Id { get; set; }

        public string ComponentType { get; set; } // set by installer
        public string MachineName { get; set; }   // set by installer
        [JsonIgnore]
        public IPAddress IPAddress { get; set; }  // set by installer
        public string NodeIPAddress { get; set; }
        public ApplicationNodeStates State { get; set; } // set by manager,
                                                         // consumed by runner
        public DateTime LastPing { get; set; } // updated by runner
        public int ActivityLevel { get; set; } // updated by runner
        public string Version { get; set; }       // set by installer
        public string RequiredDBVersion { get; set; } // set by installer
        public DateTime InstallDate { get; set; } // set by installer
        public List<string> HotfixNames { get; set; } // set by installer
        public List<NodeMetric> Metrics { get; set; } // updated by runner
        public DateTime MetricCollectionTime { get; set; } // updated by runner
        public List<NodeAlert> Alerts { get; set; } // updated by runner
        
        public LoggingConfiguration LoggingConfiguration { get; set; } // set by manager,
                                                                       // consumed by runner
        
    }
    
    /// <summary>
    /// 
    /// </summary>
    public enum ApplicationTopologyLocalConfig
    {
        CompanyKey,
        ApplicationKey,
        LogFilePath,
        ComponentType
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IApplicationTopologyManager
    {
        IEnumerable<ApplicationNode> GetTopology();
        void ResetDefaultLoggingConfiguration(string applicationNodeId);
        void ConfigureLogging(string applicationNodeId,
                                string filter = null, 
                                int level = -1, 
                                string file = null);

        void SetAlertHandled(string applicationNodeId, string alertId);
        void PauseNode(string applicationNodeId);
        void RunNode(string applicationNodeId);
        void DeleteNodeInformation(string applicationNodeId);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IApplicationTopologyInstaller
    {
        void LocalInstall(
                string id, 
                string componentType, 
                string version, 
                string requiredDB,
                string dbConnection,
                string dbName,
                string dbUN,
                string dbPW
            );
    
        string LocalUninstall(string componentType);
        void AddHotfix(string hotfixId);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IApplicationNodeGatherer
    {
        IEnumerable<NodeMetric> GatherMetrics();
        IEnumerable<NodeAlert> NewAlerts();
        int ActivityLevel { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ApplicationNodeStateEvent: EventArgs
    {
        public ApplicationNodeStates State { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IApplicationNodeRunner
    {
        void Initialize(
    
            TimeSpan updateCycle
            );

        void DoTransactNodeInfo(
            IEnumerable<NodeMetric> currentMetrics,
            DateTime metricsCollectionTime,
            IEnumerable<NodeAlert> newAlerts,
            int activityLevel,
            out ApplicationNodeStates desiredState,
            out LoggingConfiguration desiredLogging);

        event EventHandler<ApplicationNodeStateEvent> StateChanged;

        void Run();
        
        void Stop();

    }

    /// <summary>
    /// 
    /// </summary>
    public class ApplicationNodeNotInstalledException: ApplicationException
    {
        public ApplicationNodeNotInstalledException()
        {
            
        }

        public ApplicationNodeNotInstalledException(string msg): base(msg)
        {
            
        }

        public ApplicationNodeNotInstalledException(string msg, Exception inner): base(msg,inner)
        {
            
        }
    }

}