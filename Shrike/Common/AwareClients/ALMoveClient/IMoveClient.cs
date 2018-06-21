using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lok.AwareLive.Clients.Move
{
    public interface IMoveClient
    {
        void Bind(string device, string userName, string password);
        void Bind(string device, int port, string userName, string password);

        HttpStatusCode GetState(out Model.StateRec rec);
        HttpStatusCode GetHotspotEvents(DateTime start, DateTime end, out Model.HotspotEventsRec rec);
        HttpStatusCode GetBorderEvents(DateTime start, DateTime end, out Model.BorderEventsRec rec);
        HttpStatusCode GetAllEvents(DateTime start, DateTime end, out Model.EventsRec rec);
        HttpStatusCode GetListOfBorders(out Model.BorderDefinitionList rec);
        HttpStatusCode GetListOfHotspots(out Model.HotspotDefinitionList rec);
        HttpStatusCode GetSVGLayoutInfo(out Model.AreaDefinitionList areasList, out Model.LineDefinitionList linesList);

        HttpStatusCode TurnEngineOn();
        HttpStatusCode TurnEngineOff();
        HttpStatusCode SetAreasSubscription(Model.AreasSubscription areasSubscription);
        HttpStatusCode AddAreasSubscription(Model.AreasSubscription areasSubscription);
        HttpStatusCode GetListOfAreaSubscriptions(out Model.AreasSubscriptionList subscriptionList);

    }
}
