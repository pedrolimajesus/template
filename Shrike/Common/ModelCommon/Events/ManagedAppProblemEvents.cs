using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.Unik.ModelCommon.Client;

namespace Lok.Unik.ModelCommon.Events
{

    public enum ManagedAppProblemContexts
    {
        Unknown,
        LoadDataManifest,
        LoadMedia,
        ContentDeployment,
        Configure,
        Launch
    }

    public class ManagedAppProblemMessage
    {
        public string Application { get; set; }
        public string ApplicationId { get; set; }
        public DateTime ProblemTime { get; set; }
        public ManagedAppProblemContexts Context { get; set; }
        public string Problem { get; set; }
    }

    public class ManagedAppProblemEvent
    {
        public ManagedAppProblemEvent()
        {
            ProblemInfo = new ManagedAppProblemMessage();
            Identifier = Guid.NewGuid();
            DeviceTags = new List<Tag>();
        }

        

        [DocumentIdentifier]
        public Guid Identifier { get; set; }

        public string DeviceId { get; set; }

        public string DeviceName { get; set; }
        public IList<Tag> DeviceTags { get; set; }

        public ManagedAppProblemMessage ProblemInfo { get; set; }

    }
}
