using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.ControlFlow
{
    public static class Trampoline
    {
        static ConcurrentDictionary<string, Action<object>> _interceptors = new ConcurrentDictionary<string, Action<object>>();

        public static void Register(string key, Action<object> interceptor)
        {
            _interceptors[key] = interceptor;
        }

        public static void Invoke(string key, object sender)
        {
            if (_interceptors.ContainsKey(key))
                _interceptors[key](sender);
        }
    }
}
