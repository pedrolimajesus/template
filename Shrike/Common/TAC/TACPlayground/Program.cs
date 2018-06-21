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
using System.Threading;
using System.Threading.Tasks;
using AppComponents;
using AppComponents.Data;
using AppComponents.Dynamic;
using AppComponents.Dynamic.Lambdas;
using AppComponents.Dynamic.Projection;
using AppComponents.Messaging;
using AppComponents.Messaging.Declarations;
using AppComponents.Workflow;
using Newtonsoft.Json.Schema;
using TACPlaygroundClasses;


namespace TACPlayground
{
    internal enum TableDecl
    {
        Captains,
        Ships,
    }


    internal class Program
    {
        private static void Main(string[] args)
        {

            var wfst = new WorkflowSimpleTest();
            wfst.Run();
            //return;

            var tpp = new TypeProjectionPlayground();
            tpp.Stuff();


            Catalog.Services.Register(typeof (IStructuredDataStorage<>), typeof (StructuredDataStorage<>));
            Catalog.Services.Register<IStructuredDataServer>(_ => new StructuredDataServer());

            Catalog.Services.Register("CacheClient", _ =>
                                                         {
                                                             return Catalog.Preconfigure().Add(
                                                                 StructuredDataClientLocalConfig.Inbox,
                                                                 new MemoryMappedTransferInbox
                                                                     (Guid.NewGuid().ToString()))
                                                                 .Add(
                                                                     StructuredDataClientLocalConfig.Server,
                                                                     new MemoryMappedTransferOutbox("memserver"))
                                                                 .ConfiguredCreate<IStructuredDataClient>(
                                                                     () => new StructuredDataClient());
                                                         });
            Catalog.Services.Register<IMessageBusSpecifier>(_ => new IpcMessageBus());
            Catalog.Services.Register<IMessageListener>(_ => new IpcMessageListener());
            Catalog.Services.Register<IMessagePublisher>(_ => new IpcMessagePublisher());


            var sds = Catalog.Preconfigure()
                .Add(StructuredDataStorageLocalConfig.Store, new MemoryPersistentStore<string>())
                .Add(StructuredDataStorageLocalConfig.Cloner,
                     Return<string>.Arguments((string skey) => string.Copy(skey)))
                .Add(StructuredDataStorageLocalConfig.Comparer, StringComparer.CurrentCulture)
                .ConfiguredResolve<IStructuredDataStorage<string>>();

            var sdServer = Catalog.Preconfigure()
                .Add(StructuredDataServerLocalConfig.Store, sds)
                .Add(StructuredDataServerLocalConfig.Inbox, new MemoryMappedTransferInbox("memserver"))
                .Add(StructuredDataServerLocalConfig.OutboxFactory,
                     Return<IMessageOutbox>.Arguments<string>(name => new MemoryMappedTransferOutbox(name)))
                .ConfiguredResolve<IStructuredDataServer>();

            var sdsClient = Catalog.Factory.Resolve<IStructuredDataClient>("CacheClient");

            var cts = new CancellationTokenSource();
            var declType = typeof (TableDecl);
            var runServer = Task.Factory.StartNew(() => sdServer.Run(declType, cts.Token));

            var captains = sdsClient.OpenTable<Captain>(TableDecl.Captains);
            var ships = sdsClient.OpenTable<Ship>(TableDecl.Ships);
            using (sdsClient.BeginTransaction())
            {
                captains.AddNew("kirk", new Captain {Name = "Kirk", Braveness = 5});
                captains.AddNew("picard", new Captain {Name = "Picard", Braveness = 6});

                sdsClient.Commit();
            }

            var data = captains.Fetch("kirk", "picard");
            var kirk = data.Single(d => d.Key == "kirk");
            kirk.Data.Braveness = 6;

            using (sdsClient.BeginTransaction())
            {
                captains.Update("kirk", kirk.Data, kirk.ETag);
                sdsClient.Commit();
            }


            var qServerDataStorage = Catalog.Preconfigure()
                .Add(StructuredDataStorageLocalConfig.Store, new MemoryPersistentStore<string>())
                .Add(StructuredDataStorageLocalConfig.Cloner,
                     Return<string>.Arguments((string skey) => string.Copy(skey)))
                .Add(StructuredDataStorageLocalConfig.Comparer, StringComparer.CurrentCulture)
                .ConfiguredResolve<IStructuredDataStorage<string>>();

            var qServer = Catalog.Preconfigure()
                .Add(StructuredDataServerLocalConfig.Store, qServerDataStorage)
                .Add(StructuredDataServerLocalConfig.Inbox, new MemoryMappedTransferInbox("qserver"))
                .Add(StructuredDataServerLocalConfig.OutboxFactory,
                     Return<IMessageOutbox>.Arguments<string>(name => new MemoryMappedTransferOutbox(name)))
                .ConfiguredResolve<IStructuredDataServer>();

            var runQServer = Task.Factory.StartNew(() => qServer.Run(typeof (AMQPDeclarations), cts.Token));

            var specifier = Catalog.Preconfigure()
                .Add(MessageBusSpecifierLocalConfig.HostConnectionString, "qserver")
                .ConfiguredResolve<IMessageBusSpecifier>();

            specifier.DeclareExchange("msgExchange", ExchangeTypes.Direct)
                .SpecifyExchange("msgExchange").DeclareQueue("testq", "testroute");

            var listener = Catalog.Preconfigure()
                .Add(MessageListenerLocalConfig.HostConnectionString, "qserver")
                .Add(MessageListenerLocalConfig.ExchangeName, "msgExchange")
                .Add(MessageListenerLocalConfig.QueueName, "testq")
                .ConfiguredResolve<IMessageListener>();

            listener.Listen(new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>
                                (typeof (Captain),
                                 ShowCaptain));

            var sender = Catalog.Preconfigure()
                .Add(MessagePublisherLocalConfig.HostConnectionString, "qserver")
                .Add(MessagePublisherLocalConfig.ExchangeName, "msgExchange")
                .ConfiguredResolve<IMessagePublisher>();

            sender.Send(kirk.Data, "testroute");
            Console.ReadLine();

            cts.Cancel();
        }


        private static void ShowCaptain(object data, CancellationToken ct, IMessageAcknowledge ack)
        {
            var c = (Captain) data;
            Debug.WriteLine(c.Name);
            ack.MessageAcknowledged();
        }
    }


    internal class CrazyAspect : Aspect
    {
        public override bool InterceptBefore(Invocation invocation, object target, ShapeableExpando extensions,
                                             out object resultData)
        {
            resultData = null;
            return true;
        }
    }

    internal class DoStuffAspect : Aspect
    {
        public override bool InterceptAfter(Invocation invocation, object target, ShapeableExpando extensions,
                                            out object resultData)
        {
            resultData = null;
            return true;
        }
    }


    internal enum MemberNames
    {
        MyShip,
        MyCaptain
    };

    internal class TypeProjectionPlayground
    {
        public void Stuff()
        {
            var createShipFactory =
                AspectWeaver.Of(() => new Ship())
                    .WeaveAspects(
                        new AspectProvider(new DoStuffAspect(), new CrazyAspect()),
                        t => t.Methods(
                            s => s.LockOnTarget(null),
                            s => s.RecieveMessage(null, false),
                            s => s.FullStop()),
                        t => t.Properties(
                            s => s.Armor,
                            s => s.Captain)
                    )
                    .WeaveAspects(
                        new AspectProvider(new CrazyAspect()),
                        t => t.Properties(
                            s => s.HullIntegrity
                                 )
                    )
                    .CreateFactory<IShip>();

            Catalog.Services.Register(_ => createShipFactory());
            var ship = Catalog.Factory.Resolve<IShip>();

        }
    }

    
}