using System;
using System.Net;

namespace Lok.AwareLive.Clients.Look
{
    public interface ILookClient
    {
        void Bind(string device, string userName, string password);

        HttpStatusCode GetState(out Model.StateRec rec);
        HttpStatusCode GetSnapshot(out Model.SnapshotRec rec);
        HttpStatusCode GetFacesNow(out Model.FacesRec faces);
        HttpStatusCode GetFaces(DateTime start, DateTime end, out Model.FacesHistoricRec faces);
    }
}
