using System;
using System.Linq;

namespace AppComponents.ControlFlow
{
    public enum MessageQueueApplicationAlertLocalConfig
    {
        Publisher, Route
    }

    public class AlertMsg
    {
        public ApplicationAlertKind Kind { get; set; }
        public string[] Details { get; set; }
    }

    public class MessageQueueApplicationAlert : IApplicationAlert
    {
        IMessagePublisher _publisher;
        string _route;

        public MessageQueueApplicationAlert()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _publisher = cf.Get<IMessagePublisher>(MessageQueueApplicationAlertLocalConfig.Publisher);
            _route = cf[MessageQueueApplicationAlertLocalConfig.Route];
        }

        #region IApplicationAlert implementation
        public void RaiseAlert(ApplicationAlertKind kind, params object[] details)
        {
            _publisher.Send(new AlertMsg
            {
                Kind = kind,
                Details = details.Select(dt => dt.ToString()).ToArray()
            }, _route);
        }
        #endregion
    }
}
