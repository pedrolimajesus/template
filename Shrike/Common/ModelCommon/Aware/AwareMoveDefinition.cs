using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents;
using Lok.AwareLive.Clients.Move.Model;

namespace Lok.Unik.ModelCommon.Aware
{
    public class AwareMoveDefinition
    {
        public string Id { get; set; }
        public string IpAddress { get; set; }
        public BorderDefinitionList BorderDefinitions { get; set; }
        public HotspotDefinitionList HotspotDefinitions { get; set; }
    }
}
