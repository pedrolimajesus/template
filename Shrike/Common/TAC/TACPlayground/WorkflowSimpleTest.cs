using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using AppComponents;
using AppComponents.ControlFlow;
using AppComponents.Data;
using AppComponents.Files;
using AppComponents.Messaging;
using AppComponents.Workflow;
using Newtonsoft.Json;
using PluginContracts;

namespace TACPlayground
{
    internal class TestData
    {
        public string InternalProperty { get; set; }
    }

    public class WorkflowSimpleTest: IObjectAssemblySpecifier
    {
       

        private string _jsonTemplate;
        private IWorkflowCatalog _workflowCatalog;
        private WorkflowHost _host;
        private IWorkflow _wf;
        private CancellationTokenSource _cts = new CancellationTokenSource(); 

        public void Run()
        {
            
            ClassLogger.Configure();

            
            DeclareWorkflow();
            CreateHost();
            InstantiateWorkflow();
            DriveWorkflow();
            Report();

        }



        void DeclareWorkflow()
        {
            var wft = new WorkflowTemplate
            {
                Name = "TestWorkflow",
                AutoSleepSeconds = 600,
                Version = "1.0",
                Workspace = new Workspace
                                {
                                    KeyAliases = new List<Alias>(new Alias[]
                                        {
                                            new Alias { AliasKey = "Thingy", Key = "ItemKey"}
                                        }),
                                    Inputs = new List<InputDefinition>(new[]
                                        {
                                            new InputDefinition
                                                {
                                                    WorkspaceKey = "Input0",
                                                    Description = "test description",
                                                    Optional = false,
                                                    Type = "stuff"
                                                },
                                            new InputDefinition
                                                {
                                                    WorkspaceKey = "CollectionTest",
                                                    Description = "test description",
                                                    Optional = false,
                                                    Type = "stuff"
                                                }
                                        })
                                },
                Fallthrough = "TestMachine",
                Plugins = new List<PluginTemplate>(new[]
                                {
                                    new PluginTemplate
                                    {
                                        Instancename = "sys",
                                        Route = "IntraTalk"
                                    },
                                    
                                    new PluginTemplate
                                    {
                                        Instancename = "Debug",
                                        Route = "DiagnosticWorker"
                                    }
                                }),
                StateMachines = new List<StateMachineTemplate>(new[]
                                {
                                        new StateMachineTemplate
                                        {
                                            Name = "TestMachine",
                                            ActivationTrigger = "Next",
                                            InitialState = "TestInvoke",
                                            States = new List<StateTemplate>(new []
                                            {
                                                new StateTemplate
                                                    {
                                                        Name = "TestInvoke",
                                                        
                                                        EntryActions = new List<EntryActionTemplate>(new []
                                                        {
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker = "Debug",
                                                                    Route = "DiagnosticWorker/DiagnosticInvoke/{config(testkey)}/{workspace(Input0)}",
                                                                    ExceptionTrigger = "DumpReport"
                                                                }
                                                        }),
                                                        
                                                        Transitions = new List<Transition>(new []
                                                        {
                                                            new Transition
                                                                {
                                                                    Trigger = "Next",
                                                                    Next = "TestGuard"
                                                                }
                                                        })
                                                    },
                                                    new StateTemplate{
                                                        Name = "TestGuard",
                                                        
                                                        EntryActions = new List<EntryActionTemplate>(new []
                                                        {
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker ="Debug",
                                                                    Route = "DiagnosticWorker/DiagnosticInvoke/{config(testkey)}/{workspace(Input0)}",
                                                                    ExceptionTrigger = "DumpReport",
                                                                    AutoNextTrigger = "Next"
                                                                }
                                                        }),
                                                        
                                                        Transitions = new List<Transition>(new []
                                                        {
                                                            new Transition
                                                                {
                                                                    Trigger = "Next",
                                                                    ConditionTemplate = new ConditionTemplate
                                                                                            {
                                                                                                Worker = "Debug",
                                                                                                Route = "DiagnosticWorker/DiagnosticGuard/Test"
                                                                                            },
                                                                    Next = "TestDynamic"
                                                                }
                                                        })
                                                    },
                                                    new StateTemplate{
                                                        Name = "TestDynamic",
                                                        
                                                        EntryActions = new List<EntryActionTemplate>(new []
                                                        {
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker = "Debug",
                                                                    Route = "DiagnosticWorker/DiagnosticInvoke/{config(testkey)}/{workspace(Input0)}",
                                                                    ExceptionTrigger = "DumpReport",
                                                                    AutoNextTrigger = "Next"
                                                                }
                                                        }),
                                                        
                                                        Transitions = new List<Transition>(new []
                                                        {
                                                            new Transition
                                                                {
                                                                    Trigger = "Next",
                                                                    DynamicNextTemplate = new DynamicNextTemplate
                                                                                              {
                                                                                                  Worker = "Debug",
                                                                                                  Route = "DiagnosticWorker/DiagnosticDecide/Test"
                                                                                              }
                                                                }
                                                        })
                                                    },
                                                    new StateTemplate{
                                                        Name = "Approve",
                                                        
                                                        EntryActions = new List<EntryActionTemplate>(new []
                                                        {
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker = "sys",
                                                                    Route = "IntraTalk/LoadNextItem/CollectionTest/ItemKey/TestMachine/Load/NoMore",
                                                                    ExceptionTrigger = "DumpReport"
                                                             
                                                                }
                                                        }),
                                                        
                                                        Transitions = new List<Transition>(new []
                                                        {
                                                            new Transition
                                                                {
                                                                    Trigger = "Load",
                                                                    Next = "Loaded"
                                                                },
                                                            new Transition
                                                                {
                                                                    Trigger = "NoMore",
                                                                    Next = "NoMore"
                                                                }

                                                        })
                                                    },
                                                    new StateTemplate{
                                                        Name = "Loaded",
                                                        
                                                        EntryActions = new List<EntryActionTemplate>(new []
                                                        {
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker = "sys",
                                                                    Route = "IntraTalk/UnpackJsonProperties/ItemKey",
                                                                    ExceptionTrigger = "DumpReport",
                                                                    
                                                                },
                                                            new EntryActionTemplate
                                                                {
                                                                    Worker = "Debug",
                                                                    Route = "DiagnosticWorker/DiagnosticInvoke/{workspace(ItemKey.InternalProperty)}",
                                                                    AutoNextTrigger = "Next"
                                                                }
                                                        }),
                                                        
                                                        Transitions = new List<Transition>(new []
                                                        {
                                                            new Transition
                                                                {
                                                                    Trigger = "Next",
                                                                    Next = "Approve"
                                                                }
                                                        })
                                                    },
                                                    new StateTemplate
                                                    {
                                                        Name = "NoMore"
                                                    },
                                                    new StateTemplate
                                                        {
                                                            Name="DumpReport"
                                                        }
                                                   
                                                                                                      
                                            })
                                        }
                                })
            };


            _jsonTemplate = wft.ToJson();

        }

        

        void CreateHost()
        {
            _host = Catalog.Preconfigure()
                .Add(WorkflowConfiguration.WorkflowDataRepositoryKey, "Workflow")
                .Add(WorkflowConfiguration.WorkflowMessagingKey, "Workflow")
                .Add(WorkflowConfiguration.WorkflowWorkspaceKey, "Workflow")
                .ConfiguredCreate(() => new WorkflowHost());

            _host.Initialize(_cts.Token);
            _host.OnStart();
        }


        void InstantiateWorkflow()
        {
            _workflowCatalog = Catalog.Factory.Resolve<IWorkflowCatalog>();

            var testCollection = new[] { new TestData { InternalProperty = "TestData0"}, new TestData { InternalProperty = "TestData1"}};

            var init = new Dictionary<string, string>();
            init.Add("Input0", "TestInput0");
            init.Add("CollectionTest", JsonConvert.SerializeObject(testCollection) );
            string initialData = JsonConvert.SerializeObject(init);

            var id= _workflowCatalog.CreateInstanceForDefinition(initialData, _jsonTemplate);
           
            _wf = _workflowCatalog.OpenInstance(id);
            while (_wf.Status == WorkflowStatus.NoInstance)
            {
                System.Threading.Thread.Sleep(1000);
            }
            
        }

        void DriveWorkflow()
        {
            string st;
            do
            {
                System.Threading.Thread.Sleep(1000);
                st = _wf.GetState("TestMachine");

            } while (st != "NoMore");
            _host.OnStop();
        }

        void Report()
        {

            
        }



        public void RegisterIn(IObjectAssemblyRegistry registry)
        {
            // config
            registry.Register<IConfig>(_ => new ApplicationConfiguration()).AsAssemblerSingleton();

            // string resources cache
            registry.Register<StringResourcesCache>(_ => new StringResourcesCache()).AsAssemblerSingleton();

            // application alert
            registry.Register<IApplicationAlert>(_ => new AppEventLogApplicationAlert());

            // file mirror
            registry.Register<ILocalFileMirror>(_ => new CentralFileMirror());

            // host environment
            registry.Register<IHostEnvironment>(_ => new DataCenterHostEnvironment());
            
            // workspace
            registry.Register<IWorkspace>("Workflow", _ => new MemoryWorkspace());

            // scheduler, job work
            registry.Register<IJobScheduler>(_ => new InMemoryJobScheduler());

            // distributed mutex
            registry.Register<IDistributedMutex>(_ => new DevolvedDistributedMutex());

            // message bus
            registry.Register<IMessageBusSpecifier>("Workflow", _ => MemMessageBus.Instance());
            registry.Register<IMessagePublisher>("Workflow", _ => new MemMessagePublisher());
            registry.Register<IMessageListener>("Workflow", _ => new MemMessageListener());


            // blob
            registry.Register<IFilesContainer>(_ => new FileStoreBlobFileContainer());

            // data repository
            registry.Register("Workflow", typeof (IDataRepositoryService<,,,,,>), typeof (InMemoryDataRepositoryService<,,,,,>));
            
            
            // workflow
            registry.Register<IWorkflowCatalog>(
                _ => Catalog.Preconfigure()
                    .Add(WorkflowConfiguration.WorkflowDataRepositoryKey, "Workflow")
                    .Add(WorkflowConfiguration.WorkflowMessagingKey, "Workflow")
                    .Add(WorkflowConfiguration.WorkflowWorkspaceKey, "Workflow")
                    .ConfiguredCreate(() => new WorkflowCatalog()));
        }
    }

    [Export(typeof(IWorkflowPlugin))]
    [ExportMetadata("Route", "Debug")]
    public class DiagnosticWorkflowPlugIn: PluginBase
    {
        

        protected override IEnumerable<PluginBase.WorkerEntry> ProvideWorkers()
        {
            yield return new WorkerEntry(
                WorkerBase.RouteFor<DiagnosticWorker>(),
                (id, host, factoryRoute) => new DiagnosticWorker(id, host, factoryRoute));
        }
    }

    [WorkerRoute]
    public class DiagnosticWorker: WorkerBase
    {
        public DiagnosticWorker(string id, IWorkflowPluginHost host, string factoryRoute)
            : base(id, host, factoryRoute)
        {
        }



        [ExposeRoute(RouteKinds.Invoke)]
        void DiagnosticInvoke(string contextId, string route)
        {
            Debug.WriteLine("Invoke Action: {0} {1}",contextId, route);
        }


        [ExposeRoute(RouteKinds.Guard)]
        bool DiagnosticGuard(string context, string route, string state, string trigger)
        {
            Debug.WriteLine("Permit trigger: {0} {1} {2}", context,state,trigger);
            return true;
        }


        [ExposeRoute(RouteKinds.Decide)]
        private string DiagnosticDecide(string context, string route, string trigger, string state)
        {
            Debug.WriteLine("Decide: {0} {1} {2}", route, trigger, state);
            return CommonTriggers.Approve;
        }
    }
}
