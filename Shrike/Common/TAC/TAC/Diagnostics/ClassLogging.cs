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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using log4net;
using log4net.Core;

namespace AppComponents
{
    public class ClassLogger
    {
        private static readonly NullLog _nullLogger = new NullLog();

        private static volatile ClassLogger _singleton;
        private static object syncRoot = new Object();

        private HashSet<string> _classesOn = new HashSet<string>();
        private bool _logAll;
        private object _syncClasses = new object();

        private static ClassLogger Instance
        {
            get
            {
                if (_singleton == null)
                {
                    lock (syncRoot)
                    {
                        if (_singleton == null)
                        {
                            _singleton = new ClassLogger();
                        }
                    }
                }

                return _singleton;
            }
        }

        private void SetClassesOn(IEnumerable<string> classNames)
        {
            if (classNames.EmptyIfNull().Any())
            {
                lock (_syncClasses)
                {
                    _classesOn.Clear();
                    classNames.ForEach(s => _classesOn.Add(s));
                }
            }
        }

        private bool CheckClassName(string name)
        {
            if (_logAll)
                return true;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (_syncClasses)
            {
                return _classesOn.Contains(name);
            }
        }

        private void ClearClassNames()
        {
            lock (_syncClasses) _classesOn.Clear();
        }


        public static void SetLoggingForClasses(string classConfig)
        {
            if (!string.IsNullOrWhiteSpace(classConfig))
                Instance.SetClassesOn(
                    classConfig.Split(';', ',').Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()));
        }


        public static ILog Create(Type callingType)
        {
            if (Instance.CheckClassName(callingType.FullName))
            {
                return LogManager.GetLogger(callingType.FullName);
            }
            else
                return _nullLogger;
        }

        public static void ClearClassLogging()
        {
            Instance.ClearClassNames();
        }

        public static void Configure()
        {
            
            var config = Catalog.Factory.Resolve<IConfig>();
            Instance._logAll = false;
            var classLoggingString = config.Get(CommonConfiguration.ClassLogging, string.Empty);
            if (string.IsNullOrWhiteSpace(classLoggingString) || classLoggingString == "none")
            {
                ClearClassLogging();
            }
            else if (classLoggingString == "all")
            {
                ClearClassLogging();
                Instance._logAll = true;
            }
            else
            {
                SetLoggingForClasses(classLoggingString);
            }
        }
    }


    public class DebugOnlyLogger
    {
        private ILog _log;

        private DebugOnlyLogger()
        {
        }

        public bool IsDebugEnabled
        {
            get { return _log.IsDebugEnabled; }
        }


        public bool IsErrorEnabled
        {
            get { return _log.IsErrorEnabled; }
        }


        public bool IsFatalEnabled
        {
            get { return _log.IsFatalEnabled; }
        }


        public bool IsInfoEnabled
        {
            get { return _log.IsInfoEnabled; }
        }


        public bool IsWarnEnabled
        {
            get { return _log.IsWarnEnabled; }
        }


        public static DebugOnlyLogger Create(ILog log)
        {
            return new DebugOnlyLogger {_log = log};
        }


        [Conditional("DEBUG")]
        public void Debug(object message, Exception exception)
        {
            _log.Debug(message, exception);
        }

        [Conditional("DEBUG")]
        public void Debug(object message)
        {
            _log.Debug(message);
        }

        [Conditional("DEBUG")]
        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.DebugFormat(provider, format, args);
        }

        [Conditional("DEBUG")]
        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.DebugFormat(format, arg0, arg1, arg2);
        }

        [Conditional("DEBUG")]
        public void DebugFormat(string format, object arg0, object arg1)
        {
            _log.DebugFormat(format, arg0, arg1);
        }

        [Conditional("DEBUG")]
        public void DebugFormat(string format, object arg0)
        {
            _log.DebugFormat(format, arg0);
        }

        [Conditional("DEBUG")]
        public void DebugFormat(string format, params object[] args)
        {
            _log.DebugFormat(format, args);
        }

        [Conditional("DEBUG")]
        public void Error(object message, Exception exception)
        {
            _log.Error(message, exception);
        }

        [Conditional("DEBUG")]
        public void Error(object message)
        {
            _log.Error(message);
        }

        [Conditional("DEBUG")]
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.ErrorFormat(provider, format, args);
        }

        [Conditional("DEBUG")]
        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.ErrorFormat(format, arg0, arg1, arg2);
        }

        [Conditional("DEBUG")]
        public void ErrorFormat(string format, object arg0, object arg1)
        {
            _log.ErrorFormat(format, arg0, arg1);
        }

        [Conditional("DEBUG")]
        public void ErrorFormat(string format, object arg0)
        {
            _log.ErrorFormat(format, arg0);
        }

        [Conditional("DEBUG")]
        public void ErrorFormat(string format, params object[] args)
        {
            _log.ErrorFormat(format, args);
        }

        [Conditional("DEBUG")]
        public void Fatal(object message, Exception exception)
        {
            _log.Fatal(message, exception);
        }

        [Conditional("DEBUG")]
        public void Fatal(object message)
        {
            _log.Fatal(message);
        }

        [Conditional("DEBUG")]
        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.FatalFormat(provider, format, args);
        }

        [Conditional("DEBUG")]
        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.FatalFormat(format, arg0, arg1, arg2);
        }

        [Conditional("DEBUG")]
        public void FatalFormat(string format, object arg0, object arg1)
        {
            _log.FatalFormat(format, arg0, arg1);
        }

        [Conditional("DEBUG")]
        public void FatalFormat(string format, object arg0)
        {
            _log.FatalFormat(format, arg0);
        }

        [Conditional("DEBUG")]
        public void FatalFormat(string format, params object[] args)
        {
            _log.FatalFormat(format, args);
        }

        [Conditional("DEBUG")]
        public void Info(object message, Exception exception)
        {
            _log.Info(message, exception);
        }

        [Conditional("DEBUG")]
        public void Info(object message)
        {
            _log.Info(message);
        }

        [Conditional("DEBUG")]
        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.InfoFormat(provider, format, args);
        }

        [Conditional("DEBUG")]
        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.InfoFormat(format, arg0, arg1, arg2);
        }

        [Conditional("DEBUG")]
        public void InfoFormat(string format, object arg0, object arg1)
        {
            _log.InfoFormat(format, arg0, arg1);
        }

        [Conditional("DEBUG")]
        public void InfoFormat(string format, object arg0)
        {
            _log.InfoFormat(format, arg0);
        }

        [Conditional("DEBUG")]
        public void InfoFormat(string format, params object[] args)
        {
            _log.InfoFormat(format, args);
        }


        [Conditional("DEBUG")]
        public void Warn(object message, Exception exception)
        {
            _log.Warn(message, exception);
        }

        [Conditional("DEBUG")]
        public void Warn(object message)
        {
            _log.Warn(message);
        }

        [Conditional("DEBUG")]
        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            _log.WarnFormat(provider, format, args);
        }

        [Conditional("DEBUG")]
        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            _log.WarnFormat(format, arg0, arg1, arg2);
        }

        [Conditional("DEBUG")]
        public void WarnFormat(string format, object arg0, object arg1)
        {
            _log.WarnFormat(format, arg0, arg1);
        }

        [Conditional("DEBUG")]
        public void WarnFormat(string format, object arg0)
        {
            _log.WarnFormat(format, arg0);
        }

        [Conditional("DEBUG")]
        public void WarnFormat(string format, params object[] args)
        {
            _log.WarnFormat(format, args);
        }
    }


    internal class NullLog : ILog
    {
        #region ILog Members

        public void Debug(object message, Exception exception)
        {
        }

        public void Debug(object message)
        {
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
        }

        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
        }

        public void DebugFormat(string format, object arg0, object arg1)
        {
        }

        public void DebugFormat(string format, object arg0)
        {
        }

        public void DebugFormat(string format, params object[] args)
        {
        }

        public void Error(object message, Exception exception)
        {
        }

        public void Error(object message)
        {
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
        }

        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
        }

        public void ErrorFormat(string format, object arg0, object arg1)
        {
        }

        public void ErrorFormat(string format, object arg0)
        {
        }

        public void ErrorFormat(string format, params object[] args)
        {
        }

        public void Fatal(object message, Exception exception)
        {
        }

        public void Fatal(object message)
        {
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
        }

        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
        }

        public void FatalFormat(string format, object arg0, object arg1)
        {
        }

        public void FatalFormat(string format, object arg0)
        {
        }

        public void FatalFormat(string format, params object[] args)
        {
        }

        public void Info(object message, Exception exception)
        {
        }

        public void Info(object message)
        {
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
        }

        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
        }

        public void InfoFormat(string format, object arg0, object arg1)
        {
        }

        public void InfoFormat(string format, object arg0)
        {
        }

        public void InfoFormat(string format, params object[] args)
        {
        }

        public bool IsDebugEnabled
        {
            get { return false; }
        }

        public bool IsErrorEnabled
        {
            get { return false; }
        }

        public bool IsFatalEnabled
        {
            get { return false; }
        }

        public bool IsInfoEnabled
        {
            get { return false; }
        }

        public bool IsWarnEnabled
        {
            get { return false; }
        }

        public void Warn(object message, Exception exception)
        {
        }

        public void Warn(object message)
        {
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
        }

        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
        }

        public void WarnFormat(string format, object arg0, object arg1)
        {
        }

        public void WarnFormat(string format, object arg0)
        {
        }

        public void WarnFormat(string format, params object[] args)
        {
        }

        public ILogger Logger
        {
            get { return LogManager.GetLogger("nulllogger").Logger; }
        }

        #endregion
    }
}