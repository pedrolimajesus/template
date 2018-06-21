using System;

namespace Shrike.DAL.Manager
{
    using System.Collections.Generic;
    using System.Linq;

    using AppComponents;
    using AppComponents.ControlFlow;
    using AppComponents.Raven;

    using log4net;

    using Lok.Unik.ModelCommon.Client;

    [NamedContext("context://ContextResourceKind/UnikTenant")]
    public class GroupLinkManager
    {
        private readonly ILog _log;

        public GroupLinkManager()
        {
            _log = ClassLogger.Create(GetType());
        }

        public IEnumerable<GroupLink> GetAllGroupLink()
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = from groupLink in session.Query<GroupLink>() select groupLink;
                    return q2.ToArray();
                }
            }
        }

        public void RemoveLink(Guid id)
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var target = session.Load<GroupLink>(id);

                    if (null == target) return;

                    session.Delete(target);
                    _log.InfoFormat("{0} Grouplink has been deleted", target.Name);
                    session.SaveChanges();
                }
            }
        }

        public IEnumerable<GroupLink> GetAllGroupLinkWithGroupTwoType(TagType tagType)
        {
            using (ContextRegistry.NamedContextsFor(GetType()))
            {
                using (var session = DocumentStoreLocator.ContextualResolve())
                {
                    var q2 = session.Query<GroupLink>().Where(x => x.GroupTwo.Type == tagType).ToList();
                    return q2.ToArray();
                }
            }
        }
    }
}
