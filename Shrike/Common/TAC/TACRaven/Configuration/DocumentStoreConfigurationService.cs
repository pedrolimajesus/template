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

using System;
using System.Net;
using AppComponents.Extensions.EnumEx;
using Raven.Client;
using Raven.Client.Document;

namespace AppComponents
{


    /// <summary>
    /// Document class to store settings in Raven
    /// </summary>
    public class ApplicationGlobalSetting
    {
        /// <summary>
        /// The key used to set and retrieve the configuration setting
        /// </summary>
        [DocumentIdentifier]
        public string Id { get; set; }

        /// <summary>
        /// The configuration setting's value
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// An implementation of IConfig that persists configuration in the
    /// raven database.
    /// </summary>
    public class DocumentStoreConfigurationService : IConfig
    {
        public const string DefaultDataConnection = "DefaultDataConnection";
        private string _databaseLocation;
        private string _databaseName;
        private string _databasePW;
        private string _databaseUser;
        private DocumentStore _store;

        /// <summary>
        /// Requires that the settings DefaultDataDatabase, DefaultDataUser, DefaultDataPassword and DefaultDataConnection
        /// be defined in the given configuration parameter.
        /// The constructure does attempt to connect; be prepared for exceptions.
        /// </summary>
        /// <param name="masterConfiguration">A configuration store that has the data connection config.</param>
        public DocumentStoreConfigurationService(IConfig masterConfiguration)
        {
            _databaseName = masterConfiguration.Get(CommonConfiguration.DefaultDataDatabase, string.Empty);
            _databaseUser = masterConfiguration.Get(CommonConfiguration.DefaultDataUser, string.Empty);
            _databasePW = masterConfiguration.Get(CommonConfiguration.DefaultDataPassword, string.Empty);
            _databaseLocation = masterConfiguration[CommonConfiguration.DefaultDataConnection];

            _store = new DocumentStore();
            Initialize();
        }

        /// <summary>
        /// Uses the given connection string and database name as connection parameters to the 
        /// ApplicationGlobalSetting documenth store in Raven.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        public DocumentStoreConfigurationService(string connectionString, string databaseName = null)
        {
            _databaseName = databaseName;
            CreateStore(connectionString);
        }

        /// <summary>
        /// Uses the given documentStore object and database name as connection parameters to the 
        /// ApplicationGlobalSetting documenth store in Raven.
        /// </summary>
        /// <param name="documentStore"></param>
        /// <param name="databaseName"></param>
        public DocumentStoreConfigurationService(DocumentStore documentStore, string databaseName = null)
        {
            _databaseName = databaseName;
            _store = documentStore;
        }

        #region IConfig Members
        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration</returns>
        public bool Get(string id, bool defaultValue)
        {
            using (var session = OpenSession())
            {
                var setting = session.Load<ApplicationGlobalSetting>(id);
                if (null == setting)
                    return defaultValue;

                return bool.Parse(setting.Value);
            }
        }

        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration</returns>
        public int Get(string id, int defaultValue)
        {
            using (var session = OpenSession())
            {
                var setting = session.Load<ApplicationGlobalSetting>(id);
                if (null == setting)
                    return defaultValue;
                return int.Parse(setting.Value);
            }
        }

        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration item.</returns>
        public string Get(string id, string defaultValue)
        {
            using (var session = OpenSession())
            {
                var setting = session.Load<ApplicationGlobalSetting>(id);
                if (null == setting)
                    return defaultValue;
                return setting.Value;
            }
        }

        /// <summary>
        /// Indexer can get configuration settings by Id; set is not supported.
        /// If not found, throws a SettingNotFoundException.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The value of the configuration item.</returns>
        public string this[string id]
        {
            get
            {
                using (var session = OpenSession())
                {
                    var setting = session.Load<ApplicationGlobalSetting>(id);
                    if (null == setting)
                        throw new SettingNotFoundException(string.Format("setting {0} not found", id.EnumName()));
                    return setting.Value;
                }
            }
        }

        /// <summary>
        /// Attempts to load and convert the setting identified.
        /// Uses Convert.ChangeType to the given type parameter.
        /// </summary>
        /// <typeparam name="T">Type to convert to.</typeparam>
        /// <param name="id">key of config to load</param>
        /// <returns>The converted value of the configuration item.</returns>
        public T Get<T>(string id)
        {
            using (var session = OpenSession())
            {
                var setting = session.Load<ApplicationGlobalSetting>(id);
                if (null == setting)
                {
                    throw new SettingNotFoundException(string.Format("setting {0} not found", id.EnumName()));
                }
                if (typeof (T) == typeof (string))
                {
                    return (T) (object) setting.Value;
                }

                return (T) Convert.ChangeType(setting.Value, typeof (T));
            }
        }

        /// <summary>
        /// Checks to see if the given setting exists in the store
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if the setting exists.</returns>
        public bool SettingExists(string id)
        {
            using (var session = OpenSession())
            {
                var setting = session.Load<ApplicationGlobalSetting>(id);
                return null != setting;
            }
        }

        /// <summary>
        /// Indexer can get configuration settings by Id; set is not supported.
        /// If not found, throws a SettingNotFoundException.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The value of the configuration item.</returns>
        public string this[Enum id]
        {
            get { return this[id.EnumName()]; }
        }

        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration</returns>
        public bool Get(Enum id, bool defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration</returns>
        public int Get(Enum id, int defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        /// <summary>
        /// Gets the requested setting by id. If none exists,
        /// returns the default value.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="defaultValue"></param>
        /// <returns>The value of the configuration</returns>
        public string Get(Enum id, string defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        /// <summary>
        /// Attempts to load and convert the setting identified.
        /// Uses Convert.ChangeType to the given type parameter.
        /// </summary>
        /// <typeparam name="T">Type to convert to.</typeparam>
        /// <param name="id">key of config to load</param>
        /// <returns>The converted value of the configuration item.</returns>
        public T Get<T>(Enum id)
        {
            return Get<T>(id.EnumName());
        }

        /// <summary>
        /// Checks to see if the given setting exists in the store
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if the setting exists.</returns>
        public bool SettingExists(Enum id)
        {
            return SettingExists(id.EnumName());
        }

        #endregion

        private void CreateStore(string connectionString)
        {
            _store = new DocumentStore {ConnectionStringName = connectionString};
            Initialize();
        }

        private void Initialize()
        {
            if (!string.IsNullOrEmpty(_databaseUser) && !string.IsNullOrEmpty(_databasePW))
            {
                var cred = new NetworkCredential(_databaseUser, _databasePW);
                _store.Credentials = cred;
            }

            _store.Url = _databaseLocation;

            _store.Initialize();
        }

        private IDocumentSession OpenSession()
        {
            if (string.IsNullOrEmpty(_databaseName))
            {
                return _store.OpenSession();
            }

            var session = _store.OpenSession(_databaseName);
            return session;
        }
    }
}