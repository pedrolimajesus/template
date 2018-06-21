using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Raven;
using AppComponents.Workflow;
using Raven.Client.Indexes;

namespace TAC.Workflow.Raven
{
    public class MachineState_ByMachine : AbstractIndexCreationTask<MachineState>
    {
        public MachineState_ByMachine()
        {
            Map = machineStates => from ms in machineStates select new { ms.Machine };
        }
    }

    public class WorkflowMachineState_ByParent : AbstractIndexCreationTask<WorkflowMachineState>
    {
        public WorkflowMachineState_ByParent()
        {
            Map = wfmsr => from wfms in wfmsr select new { wfms.Parent };
        }
    }

    public class WorkflowMachineState_ByStateMachine : AbstractIndexCreationTask<WorkflowMachineState>
    {
        public WorkflowMachineState_ByStateMachine()
        {
            Map = wfmsr => from wfms in wfmsr select new { wfms.StateMachine };
        }
    }

    public class WorkflowInstanceInfo_ByTemplateName : AbstractIndexCreationTask<WorkflowInstanceInfo>
    {
        public WorkflowInstanceInfo_ByTemplateName()
        {
            Map = wfiir => from wfii in wfiir select new { wfii.TemplateName };
        }
    }

    public class WorkflowInstanceInfo_ByStatus : AbstractIndexCreationTask<WorkflowInstanceInfo>
    {
        public WorkflowInstanceInfo_ByStatus()
        {
            Map = wfiir => from wfii in wfiir select new { wfii.Status };
        }
    }

    public class WorkflowInstanceInfo_ByNextActivationTime : AbstractIndexCreationTask<WorkflowInstanceInfo>
    {
        public WorkflowInstanceInfo_ByNextActivationTime()
        {
            Map = wfiir => from wfii in wfiir select new { wfii.NextActivationTime };
        }
    }

    public class WorkflowEntitiesTag : AbstractIndexCreationGroupAssemblyTag
    {
    }

    public class WorkflowTrigger_ByTriggerId : AbstractIndexCreationTask<WorkflowTrigger>
    {
        public WorkflowTrigger_ByTriggerId()
        {
            Map = wfts => from wft in wfts select new { wft.TriggerId };
        }
    }

    public class WorkflowTrigger_ByInstanceTarget : AbstractIndexCreationTask<WorkflowTrigger>
    {
        public WorkflowTrigger_ByInstanceTarget()
        {
            Map = wfts => from wft in wfts select new { wft.InstanceTarget };
        }
    }

    public class WorkflowTrigger_ByRoute : AbstractIndexCreationTask<WorkflowTrigger>
    {
        public WorkflowTrigger_ByRoute()
        {
            Map = wfts => from wft in wfts select new { wft.Route };
        }
    }

    public class WorkflowTrigger_ByMachineContext : AbstractIndexCreationTask<WorkflowTrigger>
    {
        public WorkflowTrigger_ByMachineContext()
        {
            Map = wfts => from wft in wfts select new { wft.MachineContext };
        }
    }

    public class WorkflowTrigger_ByTriggerName : AbstractIndexCreationTask<WorkflowTrigger>
    {
        public WorkflowTrigger_ByTriggerName()
        {
            Map = wfts => from wft in wfts select new { wft.TriggerName };
        }
    }
}
