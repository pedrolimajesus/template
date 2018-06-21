using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using AppComponents.Extensions.ExceptionEx;

namespace Shrike.ExceptionHandling.Logic
{
    using System;

    using AppComponents;

    using Exceptions;

    public static class ExceptionHandler
    {
        public static bool Manage(Exception exception, object target, Layer codeLayer = Layer.Unknown)
        {
            var uiLogicException = exception as UILogicException;

            //not managed at ui layer, ui layer should check this
            if (uiLogicException != null && codeLayer == Layer.UILogic) return false;

            if (uiLogicException == null && codeLayer == Layer.UILogic)
            {
                LogException(exception, target);

                return true;
            }

            if (uiLogicException == null && codeLayer != Layer.UILogic)
            {
                if (codeLayer == Layer.Unknown)
                {
                    LogException(exception, target);

                    throw new UILogicException("An exception has happen, review the logs", exception);
                }
                throw exception;
            }

            return false;
        }

        private static void LogException(Exception exception, object target)
        {
            var message = string.Empty;
            if (HttpContext.Current != null)
            {
                message = string.Format("Url: {0}{1}", HttpContext.Current.Request.Url, Environment.NewLine);
            }

            message = message + exception.Message + Environment.NewLine + exception.TraceInformation();

            var reflectionException = exception as ReflectionTypeLoadException;
            if (reflectionException != null)
            {
                var sb2 = new StringBuilder();
                foreach (var exSub in reflectionException.LoaderExceptions)
                {
                    sb2.AppendLine(exSub.Message);
                    
                    var exFileNotFound = exSub as FileNotFoundException;
                    if (exFileNotFound != null)
                    {
                        if (!string.IsNullOrEmpty(exFileNotFound.FusionLog))
                        {
                            sb2.AppendLine("Fusion Log:");
                            sb2.AppendLine(exFileNotFound.FusionLog);
                        }
                    }
                    sb2.AppendLine();
                }

                // The message indicating the library or file is write in the project log.
                message = message + " Reflection Exception: " + sb2;
            }

            var logger = ClassLogger.Create(target.GetType());
            logger.Error(message);

            var aa = Catalog.Factory.Resolve<IApplicationAlert>();
            aa.RaiseAlert(ApplicationAlertKind.System, exception.TraceInformation());
        }
    }
}