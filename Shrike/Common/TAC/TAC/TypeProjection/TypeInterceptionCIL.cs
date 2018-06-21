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
using System.Reflection;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;

namespace AppComponents.TypeInterception
{
    public struct MethodContextInfo
    {
        private AssemblyName m_AsmName;
        private String m_MethodName;
        private String m_TypeName;

        public MethodContextInfo(AssemblyName AssemblyInformation, String TypeName, String MethodName)
        {
            m_AsmName = (AssemblyName) AssemblyInformation.Clone();
            m_MethodName = MethodName;
            m_TypeName = TypeName;
        }

        public AssemblyName AssemblyInformation
        {
            get { return (AssemblyName) m_AsmName.Clone(); }
        }

        public String TypeName
        {
            get { return m_TypeName; }
        }

        public String MethodName
        {
            get { return m_MethodName; }
        }
    }


    public interface IMessageNotification
    {
        void Notify(MethodContextInfo MethodInformation, Exception MethodException);
        void Notify(MethodContextInfo MethodInformation, string Message);
        void Notify(MethodContextInfo MethodInformation, object[] MethodArgs);
        void Notify(MethodContextInfo MethodInformation, object[] MethodArgs, object MethodRetVal);
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TypeInterceptionAttribute : ContextAttribute
    {
        private const string ATTRIB_NAME = "TypeInterceptionAttribute";

        //  Set the attribute name.
        public TypeInterceptionAttribute() : base(ATTRIB_NAME)
        {
        }

        //  Add our property to the context.
        public override void GetPropertiesForNewContext(IConstructionCallMessage ctorMsg)
        {
            ctorMsg.ContextProperties.Add(new TypeInterceptionProperty());
        }
    }

    public class TypeInterceptionProperty : IContextProperty, IContributeObjectSink
    {
        private const string PROP_NAME = "TypeInterceptionProperty";

        #region IContextProperty Members

        public bool IsNewContextOK(Context newCtx)
        {
            return true;
        }

        public void Freeze(Context newContext)
        {
        }

        public string Name
        {
            get { return PROP_NAME; }
        }

        #endregion

        #region IContributeObjectSink Members

        public IMessageSink GetObjectSink(MarshalByRefObject obj, IMessageSink nextSink)
        {
            return new TypeInterceptionMessage(nextSink);
        }

        #endregion
    }

    public class TypeInterceptionMessage : IMessageSink
    {
        private const string MSG_NAME = "TypeInterception";
        private const string ERROR_NO_ASYNC_PROCESSING = "Asynchronous processing is not allowed.";

        private MethodContextInfo m_MCI;
        private IMessageSink m_NextSink;
        private IMessageNotification m_Notify;

        internal TypeInterceptionMessage(IMessageSink NextSink)
        {
            m_NextSink = NextSink;
        }

        public static string ContextName
        {
            get { return MSG_NAME; }
        }

        #region IMessageSink Members

        public IMessageSink NextSink
        {
            get { return m_NextSink; }
        }

        public IMessage SyncProcessMessage(IMessage msg)
        {
            IMessage retVal = null;

            if (msg is IMethodMessage)
            {
                BeforeInvocation(msg);
                retVal = m_NextSink.SyncProcessMessage(msg);
                AfterInvocation(msg, retVal);
            }

            return retVal;
        }

        public IMessageCtrl AsyncProcessMessage(IMessage msg, IMessageSink replySink)
        {
            throw new InvalidOperationException(ERROR_NO_ASYNC_PROCESSING);
        }

        #endregion

        private void BeforeInvocation(IMessage msg)
        {
            IMethodMessage targetCall = msg as IMethodMessage;
            Type targetType = Type.GetType(targetCall.TypeName);

            m_MCI = new MethodContextInfo((AssemblyName) targetType.Assembly.GetName().Clone(),
                                          targetType.FullName, targetCall.MethodName);

            if (null != m_Notify)
            {
                m_Notify.Notify(m_MCI, targetCall.Args);
            }

            targetCall.LogicalCallContext.SetData(
                ContextName,
                this);
        }

        public void DuringInvocation(String Message)
        {
            if (null != m_Notify)
            {
                m_Notify.Notify(m_MCI, Message);
            }
        }

        public void RegisterNotify(IMessageNotification NotifyClient)
        {
            m_Notify = NotifyClient;
        }

        public void UnregisterNotify()
        {
            m_Notify = null;
        }

        private void AfterInvocation(IMessage msg, IMessage msgReturn)
        {
            IMethodReturnMessage retMsg = (IMethodReturnMessage) msgReturn;

            Exception msgEx = retMsg.Exception;
            if (msgEx != null)
            {
                if (null != m_Notify)
                {
                    m_Notify.Notify(m_MCI, msgEx);
                }
            }
            else
            {
                object retVal = null;

                if (retMsg.ReturnValue.GetType() != typeof (void))
                {
                    retVal = retMsg.ReturnValue;
                }

                if (null != m_Notify)
                {
                    m_Notify.Notify(m_MCI, retMsg.Args, retVal);
                }
            }
        }
    }
}