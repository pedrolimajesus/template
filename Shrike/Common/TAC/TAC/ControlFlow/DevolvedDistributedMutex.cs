using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace AppComponents.ControlFlow
{
    public class DevolvedDistributedMutex: IDistributedMutex
    {
        private string _name;
        private Mutex _mutex;
        private string _id;

        public DevolvedDistributedMutex()
        {
            var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = config[DistributedMutexLocalConfig.Name].ToLowerInvariant();

            _id = string.Format("Global\\{{{0}}}", _name);
            

            _mutex = new Mutex(false, _id);
            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _mutex.SetAccessControl(securitySettings);
        }

        public bool Open()
        {
            return _mutex.WaitOne(0);
        }

        public void Release()
        {
            _mutex.ReleaseMutex();
        }

        public bool Wait(TimeSpan timeout)
        {
            return _mutex.WaitOne(timeout);
        }

        public void Dispose()
        {
           
            _mutex.Dispose();
        }
    }
}
