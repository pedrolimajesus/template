using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;

namespace AppComponents.Web.ControlFlow
{
    public class WebRecurrence<T>: IRecurrence<T>
    {
        private object _lock = new object();
        private bool _running;
        private bool _stopped;
        private string _id;
        private string _coId;
        private TimeSpan _cycle;
        private Action<T> _action;
        private T _item;

        private CacheItemRemovedCallback OnCacheRemove = null;
        private CacheItemRemovedCallback OnCoRemove = null;


        public WebRecurrence()
        {
            _id = Guid.NewGuid().ToString();
            _coId = Guid.NewGuid().ToString();
            _stopped = false;
            _cycle = TimeSpan.FromSeconds(30.0);
        }


        private void AddTask(CacheItemRemovedCallback cb, string name, int seconds)
        {
            if (null == HttpRuntime.Cache.Get(name))
            {
                HttpRuntime.Cache.Insert(
                    name,
                    seconds,
                    null,
                    DateTime.UtcNow.AddSeconds(seconds),
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.NotRemovable,
                    cb);
            }
        }


        public void CacheItemRemoved(string k, object v, CacheItemRemovedReason r)
        {
            if (r == CacheItemRemovedReason.Expired)
            {
                bool isRunning;
                lock (_lock)
                {
                    if (_stopped)
                        return;

                    isRunning = _running;
                }

                if (null != _action && isRunning)
                    _action(_item);

                if (HttpRuntime.Cache.Get(_coId) == null)
                {
                    AddTask(OnCoRemove, _coId, 30);
                }

                AddTask(OnCacheRemove, k, Convert.ToInt32(v));
            }
        }

        public void CoCacheItemRemoved(string k, object v, CacheItemRemovedReason r)
        {
            if (r == CacheItemRemovedReason.Expired)
            {
                lock (_lock)
                    if (_stopped)
                        return;

                if (HttpRuntime.Cache.Get(_id) == null)
                {
                    AddTask(OnCacheRemove, _id, (int) _cycle.TotalSeconds);
                }

                if (HttpRuntime.Cache.Get(_coId) == null)
                    AddTask(OnCoRemove, k, 30);
            }
        }



        public void Recur(TimeSpan cycle, Action<T> action, T thing)
        {
            _cycle = cycle;
            _running = true;
            _stopped = false;
            _action = action;
            _item = thing;

            OnCacheRemove = new CacheItemRemovedCallback(CacheItemRemoved);
            OnCoRemove = new CacheItemRemovedCallback(CoCacheItemRemoved);

            AddTask(OnCacheRemove, _id, (int) _cycle.TotalSeconds);
            AddTask(OnCoRemove, _coId, 30);

            _action(_item);
        }

        public void Toggle()
        {
            lock (_lock) _running = !_running;

        }

        public void Stop()
        {
            lock (_lock) _stopped = true;
        }
    }
}
