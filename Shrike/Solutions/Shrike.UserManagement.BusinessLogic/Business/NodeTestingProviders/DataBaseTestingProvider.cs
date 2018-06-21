using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Topology;
using Shrike.DAL.Manager;

namespace Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders
{
    public class DataBaseTestingProvider : INodeTestingProvider
    {
        private DeploymentManager _manager;

        public DataBaseTestingProvider()
        {
            _manager = new DeploymentManager();
        }

        public bool TestNode(DatabaseInfo parameters)
        {
            String url = parameters.Url;
            string username = "";
            string password = "";
            return _manager.TestDataBaseConnection(url, username,password);
        }

        public bool TestNode(object param)
        {
            return TestNode((DatabaseInfo) param);
        }
    }
}
