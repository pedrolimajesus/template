

using System;
using System.IO;
using AppComponents;

namespace Shrike.UserManagement.BusinessLogic.Business.NodeTestingProviders
{
    public class FileServerTestingProvider : INodeTestingProvider
    {
        public bool TestNode(GlobalConfigItem parameters)
        {
            var directory = parameters.Value;
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directory);
                bool response = dirInfo.Exists;
                if (dirInfo.Exists)
                {
                    // Attempt to get a list of security permissions from the folder. 
                    // This will raise an exception if the path is read only or do not have access to view the permissions. 
                    System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(directory);
                }
                return response;
            }
            catch (UnauthorizedAccessException e)
            {
                return false;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        public bool TestNode(object parameters)
        {
            return TestNode((GlobalConfigItem)parameters);
        }
    }
}
