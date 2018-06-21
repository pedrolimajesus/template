using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;


namespace AppComponents.Topology
{
    public class LogConfigConsumer
    {
        
        private LoggingConfiguration _cachedConfiguration;
        private string _componentFile;
        

        public LogConfigConsumer(string componentFile)
        {
            _componentFile = componentFile;
            _cachedConfiguration = new LoggingConfiguration
                {
                    ClassFilter = string.Empty,
                    File = _componentFile,
                    LogLevel = 0
                };

            
        }
        
        public void ResetConfig()
        {
           
            _cachedConfiguration = new LoggingConfiguration
            {
                ClassFilter = string.Empty,
                File = _componentFile,
                LogLevel = 0
            };
        }

        public void MaybeReconfigureLogging(LoggingConfiguration _newConfig)
        {
           


            if (_newConfig.ClassFilter != null && _newConfig.ClassFilter != _cachedConfiguration.ClassFilter)
            {
                ClassLogger.SetLoggingForClasses(_newConfig.ClassFilter);
                _cachedConfiguration.ClassFilter = _newConfig.ClassFilter;
                
            }

            if (_newConfig.File != null && _newConfig.File != _cachedConfiguration.File)
            {
                SetLoggingPath(_newConfig.File);
                _cachedConfiguration.File = _newConfig.File;
            }

            if (string.IsNullOrEmpty(_newConfig.File) && _newConfig.File != _cachedConfiguration.File)
            {
                SetLoggingPath(_componentFile);
                _cachedConfiguration.File = _componentFile;
            }

            if (_newConfig.LogLevel != -1 && _newConfig.LogLevel != _cachedConfiguration.LogLevel)
            {
                TurnOnLogging(_newConfig.LogLevel);
                _cachedConfiguration.LogLevel = _newConfig.LogLevel;
            }
        }

        private void TurnOnLogging(int level)
        {

            log4net.Core.Level logLevel = log4net.Core.Level.Off;
            switch (level)
            {
                case 0:
                    logLevel = log4net.Core.Level.Error;
                    break;
                case 1:
                    logLevel = log4net.Core.Level.Warn;
                    break;
                case 2:
                    logLevel = log4net.Core.Level.Info;
                    break;
                case 3:
                    logLevel = log4net.Core.Level.All;
                    break;
            }

            log4net.Repository.ILoggerRepository[] repositories = log4net.LogManager.GetAllRepositories();

            //Configure all loggers to be at the debug level.
            foreach (log4net.Repository.ILoggerRepository repository in repositories)
            {
                repository.Threshold = logLevel;
                log4net.Repository.Hierarchy.Hierarchy hier = (log4net.Repository.Hierarchy.Hierarchy)repository;
                log4net.Core.ILogger[] loggers = hier.GetCurrentLoggers();
                foreach (log4net.Core.ILogger logger in loggers)
                {
                    ((log4net.Repository.Hierarchy.Logger)logger).Level = logLevel;
                }
            }

            //Configure the root logger.
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)log4net.LogManager.GetRepository();
            log4net.Repository.Hierarchy.Logger rootLogger = h.Root;
            rootLogger.Level = logLevel;

        }

        private void SetLoggingPath(string filePath)
        {
            log4net.Repository.Hierarchy.Hierarchy h = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
            foreach (IAppender a in h.Root.Appenders)
            {
                if (a is FileAppender)
                {
                    FileAppender fa = (FileAppender)a;
                    
                    fa.File = filePath;
                    fa.ActivateOptions();
                    break;
                }
            }
        }
    }
}
