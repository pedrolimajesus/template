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

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace AppComponents.Azure
{
    //     CloudBlobClient  client1 = Client.FromConfig().ForBlobs();  
    //     CloudQueueClient client2 = Client.FromConfig().ForQueues();  
    //     CloudTableClient client3 = Client.FromConfig().ForTables(); 

    public static class Client
    {
        static Client()
        {
            IConfig config = Catalog.Factory.Resolve<IConfig>();

            CloudStorageAccount.SetConfigurationSettingPublisher(
                (configName, configSettingPublisher) =>
                    {
                        bool isAvailable = true;
                        string connectionString;

                        if (isAvailable)
                        {
                            connectionString = RoleEnvironment.GetConfigurationSettingValue(configName);
                        }
                        else
                        {
                            connectionString = config[CommonConfiguration.DefaultStorageConnection];
                        }

                        configSettingPublisher(connectionString);
                    });
        }


        public static CloudStorageAccount FromConfig(
            string configSetting = "DataConnection")
        {
            return CloudStorageAccount.FromConfigurationSetting(configSetting);
        }


        public static CloudStorageAccount FromString(string connectionString)
        {
            return CloudStorageAccount.Parse(connectionString);
        }
    }


    public static class CloudStorageClientHelpers
    {
        public static CloudBlobClient ForBlobs(this CloudStorageAccount account)
        {
            return account.CreateCloudBlobClient();
        }

        public static CloudQueueClient ForQueues(this CloudStorageAccount account)
        {
            return account.CreateCloudQueueClient();
        }

        public static CloudTableClient ForTables(this CloudStorageAccount account)
        {
            return account.CreateCloudTableClient();
        }
    }
}