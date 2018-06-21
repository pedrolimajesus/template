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
using System.Linq;
using System.Reflection;
using System.Threading;
using AppComponents.RandomNumbers;
using Microsoft.WindowsAzure.StorageClient;
using log4net;

namespace AppComponents.Azure
{
    public class HorizonalScaleCloudQueue
    {
        private readonly String _queueBaseName;
        private readonly CloudQueueClient _queueClient;
        private readonly List<CloudQueue> _queues;
        private int _currentQ;

        private DebugOnlyLogger _dblog;
        private ILog _log;


        public HorizonalScaleCloudQueue(CloudQueueClient queueClient, string queueBaseName, int numberOfPartitions)
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);


            _queueClient = queueClient;
            _queueBaseName = queueBaseName;

            List<CloudQueue> partitions = new List<CloudQueue>();
            for (int i = 0; i < numberOfPartitions; i++)
            {
                CloudQueue q = queueClient.GetQueueReference(_queueBaseName + i);
                partitions.Add(q);
            }

            _queues = new List<CloudQueue>(Shuffle(partitions));
        }


        public HorizonalScaleCloudQueue(CloudQueueClient queueClient, string queueBaseName)
        {
            _queueClient = queueClient;
            _queueBaseName = queueBaseName;

            // discover how many partitions are there in the storage account
            List<CloudQueue> qs = new List<CloudQueue>();
            qs.Add(queueClient.GetQueueReference(_queueBaseName + 0));

            for (int index = 1;; index++)
            {
                CloudQueue q = queueClient.GetQueueReference(_queueBaseName + index);
                if (q.Exists())
                {
                    qs.Add(q);
                }
                else
                {
                    break;
                }
            }

            _queues = new List<CloudQueue>(Shuffle(qs));
        }

        #region CloudQueue implementation

        public void AddMessage(CloudQueueMessage message, TimeSpan timeToLive)
        {
            _queues[GetNextQueue()].AddMessage(message, timeToLive);
        }

        public void AddMessage(CloudQueueMessage message)
        {
            _queues[GetNextQueue()].AddMessage(message);
        }

        public IAsyncResult BeginAddMessage(CloudQueueMessage message, AsyncCallback callback, object state)
        {
            CloudQueue queue = _queues[GetNextQueue()];
            NestedAsyncHelper wrapper = NestedAsyncHelper.WrapBeginParameters(callback, state, queue);
            return wrapper.WrapAsyncResult(queue.BeginAddMessage(message, NestedAsyncHelper.Callback, wrapper));
        }

        public void EndAddMessage(IAsyncResult asyncResult)
        {
            CloudQueue queue = (CloudQueue) NestedAsyncHelper.GetExtraState(asyncResult);
            queue.EndAddMessage(NestedAsyncHelper.UnwrapAsyncResult(asyncResult));
        }

        public CloudQueueMessage GetMessage()
        {
            CloudQueue originQ = _queues[GetNextQueue()];
            CloudQueueMessage msg = originQ.GetMessage();
            return HSMessageWrapper.FromCloudQueueMessage(msg, originQ);
        }

        public CloudQueueMessage GetMessage(TimeSpan visibilityTimeout)
        {
            CloudQueue originQ = _queues[GetNextQueue()];
            CloudQueueMessage msg = originQ.GetMessage(visibilityTimeout);
            return HSMessageWrapper.FromCloudQueueMessage(msg, originQ);
        }

        public IEnumerable<CloudQueueMessage> GetMessages(int messageCount, TimeSpan visibilityTimeout)
        {
            CloudQueue originQ = _queues[GetNextQueue()];
            IEnumerable<CloudQueueMessage> msgs = originQ.GetMessages(messageCount, visibilityTimeout);
            return msgs.Select(msg => HSMessageWrapper.FromCloudQueueMessage(msg, originQ));
        }

        public IAsyncResult BeginGetMessages(int messageCount, TimeSpan visibilityTimeout, AsyncCallback callback,
                                             object state)
        {
            CloudQueue queue = _queues[GetNextQueue()];
            NestedAsyncHelper wrapper = NestedAsyncHelper.WrapBeginParameters(callback, state, queue);
            return
                wrapper.WrapAsyncResult(queue.BeginGetMessages(messageCount, visibilityTimeout,
                                                               NestedAsyncHelper.Callback, wrapper));
        }

        public IEnumerable<CloudQueueMessage> EndGetMessages(IAsyncResult asyncResult)
        {
            CloudQueue queue = (CloudQueue) NestedAsyncHelper.GetExtraState(asyncResult);
            IEnumerable<CloudQueueMessage> msgs = queue.EndGetMessages(NestedAsyncHelper.UnwrapAsyncResult(asyncResult));
            return msgs.Select(msg => HSMessageWrapper.FromCloudQueueMessage(msg, queue));
        }

        public void DeleteMessage(CloudQueueMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            HSMessageWrapper msg = (HSMessageWrapper) message;
            msg._origin.DeleteMessage(message);
        }

        public IAsyncResult BeginDeleteMessage(CloudQueueMessage message, AsyncCallback callback, object state)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            HSMessageWrapper msg = (HSMessageWrapper) message;
            NestedAsyncHelper wrapper = NestedAsyncHelper.WrapBeginParameters(callback, state, msg._origin);

            return wrapper.WrapAsyncResult(msg._origin.BeginDeleteMessage(msg, NestedAsyncHelper.Callback, wrapper));
        }

        public void EndDeleteMessage(IAsyncResult asyncResult)
        {
            CloudQueue queue = (CloudQueue) NestedAsyncHelper.GetExtraState(asyncResult);
            queue.EndDeleteMessage(NestedAsyncHelper.UnwrapAsyncResult(asyncResult));
        }

        public int RetrieveApproximateMessageCount()
        {
            return _queues.Select(o => o.RetrieveApproximateMessageCount()).Sum();
        }

        public bool CreateIfNotExist()
        {
            bool res = false;
            foreach (CloudQueue partition in _queues)
            {
                res |= partition.CreateIfNotExist();
            }
            return res;
        }

        public bool Exists()
        {
            return _queues.All(o => o.Exists());
        }

        public void Delete()
        {
            foreach (CloudQueue q in _queues)
            {
                q.Delete();
            }
        }

        #endregion

        private static IEnumerable<T> Shuffle<T>(IEnumerable<T> input)
        {
            Random rnd = GoodSeedRandom.Create();
            return input.OrderBy(x => rnd.Next());
        }

        private int GetNextQueue()
        {
            int signedres = Interlocked.Increment(ref _currentQ);
            uint unsignedres = (uint) signedres;
            return (int) (unsignedres%_queues.Count);
        }

        #region Nested type: HSMessageWrapper

        private class HSMessageWrapper : CloudQueueMessage
        {
            public CloudQueue _origin;

            public HSMessageWrapper(byte[] content)
                : base(content)
            {
            }

            public static HSMessageWrapper FromCloudQueueMessage(CloudQueueMessage msg, CloudQueue originQ)
            {
                if (msg == null)
                {
                    return null;
                }

                HSMessageWrapper result = new HSMessageWrapper(msg.AsBytes);
                FieldInfo[] fields_of_class =
                    msg.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (FieldInfo fi in fields_of_class)
                {
                    fi.SetValue(result, fi.GetValue(msg));
                }


                result._origin = originQ;

                return result;
            }
        }

        #endregion
    }
}