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

namespace PluginContracts
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExposeRouteAttribute : Attribute
    {
        public ExposeRouteAttribute(RouteKinds kind, string routeEndPoint)
        {
            Kind = kind;
            RouteEndPoint = routeEndPoint;
        }

        public ExposeRouteAttribute(RouteKinds kind)
        {
            Kind = kind;
            RouteEndPoint = null;
        }


        public RouteKinds Kind { get; private set; }
        public string RouteEndPoint { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class WorkerRouteAttribute : Attribute
    {
        public WorkerRouteAttribute(string classRoute)
        {
            WorkerRoute = classRoute;
        }

        public WorkerRouteAttribute()
        {
            WorkerRoute = null;
        }

        public string WorkerRoute { get; private set; }
    }


    public abstract class WorkerBase : IWorkflowWorker
    {
        private Type _actualType;
        private string _classRoute;

        protected Dictionary<string, Func<string, string, string, string, string>> _decides =
            new Dictionary<string, Func<string, string, string, string, string>>();

        protected string _factoryRoute;

        protected Dictionary<string, Func<string, string, string, string, bool>> _guards =
            new Dictionary<string, Func<string, string, string, string, bool>>();

        protected IWorkflowPluginHost _host;
        protected string _id;

        protected Dictionary<string, Action<string, string>> _invokes =
            new Dictionary<string, Action<string, string>>();

        protected HashSet<string> _methodNames = new HashSet<string>();

        public WorkerBase(string id, IWorkflowPluginHost host, string factoryRoute)
        {
            _id = id;
            _host = host;
            _actualType = GetType();
            _factoryRoute = factoryRoute;
            AnalyzeType();
        }

        protected Type InvokeType
        {
            get { return typeof (Action<string, string>); }
        }

        protected Type GuardType
        {
            get { return typeof (Func<string, string, string,string, bool>); }
        }

        protected Type DecideType
        {
            get { return typeof (Func<string, string, string, string, string>); }
        }

        #region IWorkflowWorker Members

        public string WorkerRoute
        {
            get { return _factoryRoute + @"/" + _classRoute; }
        }

        public virtual IEnumerable<string> ListRoutes(RouteKinds kinds = RouteKinds.All)
        {
            var routes = Enumerable.Empty<string>();

            if (kinds.HasFlag(RouteKinds.Invoke))
                routes = routes.Concat(_invokes.Keys);

            if (kinds.HasFlag(RouteKinds.Guard))
                routes = routes.Concat(_guards.Keys);

            if (kinds.HasFlag(RouteKinds.Decide))
                routes = routes.Concat(_decides.Keys);

            return routes;
        }

        public virtual bool SupportsRoute(string route)
        {
            return ListRoutes().Any(r => route.StartsWith(r));
        }

        public virtual void Invoke(string contextId, string route)
        {
            _invokes[MethodEndpoint(route)].Invoke(contextId, route);
        }

        public virtual string DecideTransition(string contextId, string route, string trigger, string state)
        {
            return _decides[MethodEndpoint(route)](contextId, route, trigger, state);
        }

        public virtual bool Guard(string contextId, string route, string state, string trigger)
        {
            return _guards[MethodEndpoint(route)](contextId, route, state, trigger);
        }

        #endregion

        public static string RouteFor<T>() where T : WorkerBase
        {
            return
                AutoSet(typeof (T).GetCustomAttributes(typeof (WorkerRouteAttribute), true).Select(
                    o => o as WorkerRouteAttribute).Single().WorkerRoute, typeof(T));
        }

        private static string AutoSet(string p, Type t)
        {
            return p ?? t.Name;
        }

        protected bool ValidateMethodSignature(RouteKinds kind, MethodInfo mi)
        {
            switch (kind)
            {
                case RouteKinds.Decide:
                    if (!DelegateCheck.IsCompatible(DecideType, mi))
                        throw new ArgumentException(string.Format("method {0}.{1} does note have the required method signature",_actualType.Name, mi.Name));
                    break;

                case RouteKinds.Guard:
                    if (!DelegateCheck.IsCompatible(GuardType, mi))
                        throw new ArgumentException(string.Format("method {0}.{1} does note have the required method signature", _actualType.Name, mi.Name));
                    break;

                case RouteKinds.Invoke:
                    if (!DelegateCheck.IsCompatible(InvokeType, mi))
                        throw new ArgumentException(string.Format("method {0}.{1} does note have the required method signature", _actualType.Name, mi.Name));
                    break;

                default:
                    break;
            }

            return true;
        }

        private string RegisterMethodName(string recorded, string fallback)
        {
            var name = (recorded ?? fallback);
            if (!_methodNames.Add(name))
            {
                // can't have duplicate routes.
                throw new ArgumentException(name);
            }
            return name;
        }

        protected virtual void AnalyzeType()
        {
            _classRoute =
                (_actualType
                    .GetCustomAttributes(typeof (WorkerRouteAttribute), true)
                    .Select(o => o as WorkerRouteAttribute)
                    .Single()
                    .WorkerRoute) ?? _actualType.Name;


            var classMethods =
                from m in
                    _actualType.GetMethods(BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public |
                                           BindingFlags.NonPublic)
                let era =
                    m.GetCustomAttributes(typeof (ExposeRouteAttribute), true).SingleOrDefault() 
                where era != null
                select new {Method = m, Expose = era as ExposeRouteAttribute};

            foreach (var cm in classMethods)
            {
                ValidateMethodSignature(cm.Expose.Kind, cm.Method);

            }

            var methods =
                (from m in classMethods
                let methodName = RegisterMethodName(m.Expose.RouteEndPoint, m.Method.Name)
                select
                    new {m = m.Method, r = m.Expose, route = _classRoute + @"/" + methodName}).ToArray();

            var invokeMethods = from m in methods
                                where m.r.Kind == RouteKinds.Invoke
                                select m;

            foreach (var routedMethod in invokeMethods)
            {
                _invokes.Add(routedMethod.route,
                             (Action<string, string>)
                             Delegate.CreateDelegate(InvokeType, this, routedMethod.m.Name));
            }

            var decideMethods = from m in methods
                                where m.r.Kind == RouteKinds.Decide
                                select m;

            foreach (var routedMethod in decideMethods)
            {
                _decides.Add(routedMethod.route,
                             (Func<string, string, string, string, string>)
                             Delegate.CreateDelegate(DecideType, this, routedMethod.m.Name));
            }

            var guardMethods = from m in methods
                               where m.r.Kind == RouteKinds.Guard
                               select m;

            foreach (var routedMethod in guardMethods)
            {
                _guards.Add(routedMethod.route,
                            (Func<string, string, string, string, bool>)
                            Delegate.CreateDelegate(GuardType, this, routedMethod.m.Name));
            }
        }

        protected string MethodEndpoint(string route)
        {
            var parts = route.Split('/');
            string methodPath = string.Empty;

            foreach (var p in parts)
            {
                methodPath += p;
                if (_methodNames.Contains(p))
                    break;
                methodPath += @"/";
            }

            return methodPath;
        }

        protected string MethodName(string route)
        {
            var methodName = string.Join(@"/", MethodEndpoint(route).Split('/').Take(2));
            return methodName;
        }


        protected IEnumerable<string> RouteArguments(string route)
        {
            var methodName = MethodName(route);
            string argPart = route.Substring(route.IndexOf(methodName) + methodName.Length);
            return argPart.Split('/').Where(ag=>!string.IsNullOrEmpty(ag));
        }

        protected Dictionary<Enum, string> ExtractValueArguments(string route, params Enum[] names)
        {
            var args = RouteArguments(route).ToArray();
            if (args.Length != names.Length)
                throw new BadRouteException(route);
            var collocated = new Dictionary<Enum, string>();
            for (int each = 0; each != args.Length; each++)
                collocated.Add(names[each], args[each]);
            return collocated;
        }
    }

    public class BadRouteException : Exception
    {
        public BadRouteException()
        {
        }

        public BadRouteException(string msg) : base(msg)
        {
        }
    }


    internal class ParameterInfoComparer : IEqualityComparer<ParameterInfo>
    {
        #region IEqualityComparer<ParameterInfo> Members

        public bool Equals(ParameterInfo x, ParameterInfo y)
        {
            return x.IsIn == y.IsIn &&
                   x.IsLcid == y.IsLcid &&
                   x.IsOut == y.IsOut &&
                   x.IsRetval == y.IsRetval &&
                   x.ParameterType.IsAssignableFrom(y.ParameterType);
        }

        public int GetHashCode(ParameterInfo obj)
        {
            return obj.IsIn.GetHashCode() ^ obj.IsLcid.GetHashCode() ^ obj.IsOut.GetHashCode() ^
                   obj.IsRetval.GetHashCode() ^ obj.ParameterType.GetHashCode();
        }

        #endregion
    }

    internal class DelegateCheck
    {
        public static bool IsCompatible(Type delegateType, MethodInfo m)
        {
            var delegateInvoke = delegateType.GetMethod("Invoke");
            return AreCompatible(delegateInvoke, m);
        }

        public static bool AreCompatible(MethodInfo method1, MethodInfo method2)
        {
            bool equal = method2.GetParameters().SequenceEqual(method1.GetParameters(), new ParameterInfoComparer());
            equal &= method1.ReturnType.IsAssignableFrom(method2.ReturnType) &&
                     (method1.ReturnType == typeof (void) || method2.ReturnType != typeof (void));
            return equal;
        }
    }
}