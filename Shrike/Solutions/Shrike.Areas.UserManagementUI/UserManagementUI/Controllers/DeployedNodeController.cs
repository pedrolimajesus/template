using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppComponents;
using Lok.Unik.ModelCommon.Command;
using Shrike.Areas.UserManagementUI.UserManagementUI.Models;

namespace Shrike.Areas.UserManagementUI.UserManagementUI.Controllers
{


    public class DeployedNodeController : Controller
    {
        private DeploymentUILogic _deployLogic;

        public DeployedNodeController()
        {
            _deployLogic = new DeploymentUILogic();
        }

        //
        // GET: /DeployedNode/
        public ActionResult Index(string nodeId)
        {
            ApplicationNode node = _deployLogic.GetApplicationNode(nodeId);
            if (node !=null)
            {
                if (node.LoggingConfiguration == null)
                {
                    node.LoggingConfiguration = new LoggingConfiguration();
                }

                if (node.Alerts == new List<NodeAlert>())
                {
                    node.Alerts = new List<NodeAlert>();
                }
                
                ViewBag.ApplicationNode = node.Id;

                return View(node);
            }

            return View();
        }

        public ActionResult ExecuteLoggingAction(LoggingConfiguration logging, string applicationNode, string command)
        {
            switch (command)
            {
                case "Restore": { logging = RestoreLoggingConfiguration(applicationNode, logging); } break;
                case "Save": { SaveLoggingNode(logging, applicationNode); } break;
            }

            ViewBag.ApplicationNode = applicationNode;
            ModelState.Clear();
            return PartialView("~/Areas/UserManagementUI/Views/DeployedNode/LoggingPartial.cshtml", logging);
        }

        private LoggingConfiguration RestoreLoggingConfiguration(string applicationNode, LoggingConfiguration logging)
        {
            try
            {
                ApplicationNode node = _deployLogic.GetApplicationNode(applicationNode);
                logging = node.LoggingConfiguration;
                ViewBag.Status = CommandStatus.RestorePassed;
                return logging;
            }
            catch (Exception)
            {
                ViewBag.Status = CommandStatus.RestoreFailed;
                return logging;
            }
        }

        private void SaveLoggingNode(LoggingConfiguration logging, string applicationNode)
        {
            try
            {
                _deployLogic.UpdateLoggingConfiguration(applicationNode, logging);
                ViewBag.Status = CommandStatus.SavePassed;
            }
            catch (Exception e)
            {
                ViewBag.Status = CommandStatus.SaveFailed;
            }
        }

        public ActionResult DeleteNode(String idNode)
        {

            bool result = true;
            try
            {
                _deployLogic.DeleteApplicationNode(idNode);
            } catch (Exception e)
            {
                result = false;
            }

            return Json(new { state = result});
        }

        public ActionResult ToggleNodeActive(string idNode)
        {
            bool result = true;
            string msg;

            try
            {
                result = _deployLogic.ApplicationNodeToggleActive(idNode);
                msg = Resource.DeployedNodeController_ToggleNodeActive_Command_sent_to_ +
                      (result
                           ? Resource.DeployedNodeController_ToggleNodeActive_Activate
                           : Resource.DeployedNodeController_ToggleNodeActive_Deactivate) +
                      Resource.DeployedNodeController_ToggleNodeActive__node__Allow_for_a_few_minutes_;
            }

            catch (Exception)
            {

                msg = Resource.DeployedNodeController_ToggleNodeActive_Command_could_not_be_sent_;
            }

            return Json(new {status = msg});
        }

        public ActionResult DeleteAlert(string idNode, string idAlert)
        {
            _deployLogic.DeleteAlert(idNode,idAlert);
            return new EmptyResult();
            
        }

    }
}
