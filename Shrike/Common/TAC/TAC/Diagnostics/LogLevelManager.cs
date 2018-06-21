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
using System.Diagnostics;
using System.Linq;
using log4net;
using log4net.Core;
using log4net.Repository;
using log4net.Repository.Hierarchy;

namespace AppComponents
{
    public static class LogLevelManager
    {
        private static string[] legitimateLevels = new[] {"off", "fatal", "error", "warn", "info", "debug", "all"};

        public static void Set(string level)
        {
            

            Debug.Assert(legitimateLevels.Contains(level.ToLower()));

            ILoggerRepository[] repositories = LogManager.GetAllRepositories();


            foreach (ILoggerRepository repository in repositories)
            {
                repository.Threshold = repository.LevelMap[level];
                Hierarchy hier = (Hierarchy) repository;
                ILogger[] loggers = hier.GetCurrentLoggers();
                foreach (ILogger logger in loggers)
                {
                    var setLevel = logger.Name == "CriticalLogAlways" ? "all" : level;

                    ((Logger) logger).Level = hier.LevelMap[setLevel];
                }
            }


            Hierarchy h = (Hierarchy) LogManager.GetRepository();
            Logger rootLogger = h.Root;
            rootLogger.Level = h.LevelMap[level];
        }
    }


    public static class CriticalLog
    {
        private static volatile ILog _singleton;
        private static object syncRoot = new Object();

        public static ILog Always
        {
            get
            {
                if (_singleton == null)
                {
                    lock (syncRoot)
                    {
                        if (_singleton == null)
                        {
                            _singleton = LogManager.GetLogger("CriticalLogAlways");
                        }
                    }
                }

                return _singleton;
            }
        }
    }
}