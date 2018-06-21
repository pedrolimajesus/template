using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using AppComponents;
using AppComponents.Topology;

using log4net;
using MvcContrib;
using Shrike.ExceptionHandling;
using Shrike.ExceptionHandling.Logic;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{

    using Models;
    using UILogic;
    using ExceptionHandling.Exceptions;

    [NamedContext("context://ContextResourceKind/ApplicationRoot")]
    [Authorize(Roles = RoleFlags.OwnerAndTenantOwners)]
    public class DeploymentController : Controller
    {

        private readonly ILog _log = ClassLogger.Create(typeof (DeploymentController));

        private readonly DeploymentUILogic _deployLogic;

        public DeploymentController()
        {
            RavenGlobalConfig.MaybeInitDefaults();
            _deployLogic = new DeploymentUILogic();
        }

        //
        // GET: /Deployment/
        [Authorize(Roles = RoleFlags.OwnerAndTenantOwners)]
        public ActionResult Index()
        {
            var model = new DeploymentObj
            {
                Database = _deployLogic.GetDataBaseConfiguration(),
                FileStore = _deployLogic.GetFileStoreConfiguration(),
                EmailServer = _deployLogic.GetEmailServerConfiguration()
            };

            var appNodeList = _deployLogic.GetAllApplicationNodes();
            ViewBag.AppNodeList = appNodeList.ToList();

            return View(model);
        }

        public ActionResult ExecuteDatabaseAction(DataBase data, string command)
        {
            var database = new DataBase();

            if (string.IsNullOrEmpty(command))
            {
                return PartialView("~/Areas/UserManagementUI/Views/Deployment/DataBasePartial.cshtml", database);
            }

            switch (command)
            {
                case "Test":
                {
                    database = TestDatabase(data);
                }
                    break;
                case "Save":
                {
                    database = SaveDatabase(data);
                }
                    break;
                case "Restore":
                {
                    database = RestoreDatabase();
                }
                    break;
            }

            return PartialView("~/Areas/UserManagementUI/Views/Deployment/DataBasePartial.cshtml", database);
        }

        private DataBase TestDatabase(DataBase data)
        {
            try
            {
                data.Status = _deployLogic.TestDataBaseConnection(data)
                    ? CommandStatus.TestPassed
                    : CommandStatus.TestFailed;
            }
            catch (Exception e)
            {
                _log.ErrorFormat("Error Testing database [{0}]", e.Message);
                data.Status = CommandStatus.TestFailed;
            }
            return data;
        }

        private DataBase SaveDatabase(DataBase data)
        {
            try
            {
                _deployLogic.SaveDatabaseConfiguration(data);
                data.Status = CommandStatus.SavePassed;
                return data;
            }
            catch (Exception e)
            {
                data.Status = CommandStatus.SaveFailed;
                return data;
            }

        }

        private DataBase RestoreDatabase()
        {
            DataBase database;
            try
            {
                database = _deployLogic.GetDataBaseConfiguration();
                database.Status = CommandStatus.RestorePassed;
            }
            catch (Exception e)
            {
                database = new DataBase
                {
                    Status = CommandStatus.RestoreFailed
                };
            }
            return database;
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
        public ActionResult ExecuteEmailServerAction(EmailServer data, string emCommand)
        {
            ModelState.Clear();

            EmailServer emailServer = null;

            if (string.IsNullOrEmpty(emCommand))
            {
                return PartialView("~/Areas/UserManagementUI/Views/Deployment/EmailServerPartial.cshtml",
                    new EmailServer());
            }

            switch (emCommand)
            {
                case "Test":
                {
                    emailServer = TestEmailServer(data);
                }
                    break;

                case "Save":
                {
                    emailServer = SaveEmailServer(data);
                }

                    break;

                case "Restore":
                {
                    emailServer = RestoreEmailServer();
                }
                    break;
            }

            return Request.IsAjax()
                ? (ActionResult)
                    PartialView("~/Areas/UserManagementUI/Views/Deployment/EmailServerPartial.cshtml", emailServer)
                : (ActionResult)
                    View("~/Areas/UserManagementUI/Views/Deployment/EmailServerPartial.cshtml", emailServer);
        }

        public ActionResult TestEmailServer()
        {
            return PartialView("~/Areas/UserManagementUI/Views/Deployment/TestEmailServer.cshtml");
        }

        [HttpPost]
        public ActionResult TestEmailServer(TestEmailServer testEmailServer)
        {
            var emailServer = TestEmailServer(
                new EmailServer
                {
                    IsSsl = testEmailServer.IsSsl,
                    Password = testEmailServer.Password,
                    Port = testEmailServer.Port,
                    SmtpServer = testEmailServer.SmtpServer,
                    Status = testEmailServer.Status,
                    Username = testEmailServer.Username,
                    ReplyAddress = testEmailServer.ReplyAddress
                }, testEmailServer.TestEmailAddress);

            return Json(emailServer.Status == CommandStatus.TestFailed ? Boolean.FalseString : Boolean.TrueString);
        }

        private EmailServer RestoreEmailServer()
        {
            EmailServer emailServer;

            try
            {
                emailServer = _deployLogic.GetEmailServerConfiguration();
                emailServer.Status = CommandStatus.RestorePassed;
            }
            catch (Exception exception)
            {
                ExceptionHandler.Manage(exception, this, Layer.UILogic);
                emailServer = new EmailServer {Status = CommandStatus.RestoreFailed};
                _log.ErrorFormat("Restore Failed.");
            }

            return emailServer;
        }

        private EmailServer SaveEmailServer(EmailServer data)
        {
            try
            {
                _deployLogic.SaveEmailServerConfiguration(data);
                data.Status = CommandStatus.SavePassed;
            }
            catch (SmtpException exception)
            {
                ExceptionHandler.Manage(exception, this, Layer.UILogic);

                data.Status = CommandStatus.SaveFailed;
                _log.ErrorFormat("Save Failed.");
            }

            return data;
        }

        private EmailServer TestEmailServer(EmailServer data, string email = null)
        {
            try
            {
                data.Status = _deployLogic.TestEmailServerConnection(data, email)
                    ? CommandStatus.TestPassed
                    : CommandStatus.TestFailed;
            }

            catch (SmtpException exception)
            {
                ExceptionHandler.Manage(exception, this, Layer.UILogic);

                data.Status = CommandStatus.TestFailed;
                _log.ErrorFormat("Test Failed.");
            }

            return data;

        }

        public ActionResult ExecuteFileStoreAction(FileStore data, string fsCommand)
        {
            var filestore = new FileStore();

            if (string.IsNullOrEmpty(fsCommand))
            {
                return PartialView("~/Areas/UserManagementUI/Views/Deployment/FileStorePartial.cshtml", filestore);
            }

            switch (fsCommand)
            {
                case "Test":
                {
                    filestore = TestFileStore(data);
                }
                    break;
                case "Save":
                {
                    filestore = SaveFileStore(data);
                }
                    break;
                case "Restore":
                {
                    filestore = RestoreFileStore();
                }
                    break;
            }
            return PartialView("~/Areas/UserManagementUI/Views/Deployment/FileStorePartial.cshtml", filestore);
        }

        private FileStore RestoreFileStore()
        {
            FileStore fs;
            try
            {
                fs = _deployLogic.GetFileStoreConfiguration();
                fs.Status = CommandStatus.RestorePassed;
            }
            catch (Exception e)
            {
                fs = new FileStore();
                fs.Status = CommandStatus.RestoreFailed;
            }

            return fs;
        }

        private FileStore SaveFileStore(FileStore data)
        {
            try
            {
                _deployLogic.SaveFileStoreConfiguration(data);
                data.Status = CommandStatus.SavePassed;
                return data;
            }
            catch (Exception e)
            {
                data.Status = CommandStatus.SaveFailed;
                return data;
            }
        }

        private FileStore TestFileStore(FileStore fStore)
        {
            try
            {
                fStore.Status = _deployLogic.TestFileStoreConnection(fStore)
                    ? CommandStatus.TestPassed
                    : CommandStatus.TestFailed;
            }
            catch (Exception)
            {
                fStore.Status = CommandStatus.TestFailed;
            }

            return fStore;
        }

    }
}