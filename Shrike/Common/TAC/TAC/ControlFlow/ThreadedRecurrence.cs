using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppComponents;

namespace AppComponents.ControlFlow
{
    public class ThreadedRecurrence<T> : IRecurrence<T>
    {

        private TimeSpan _cycle;
        private Action<T> _action;
        private T _thing;
        private bool _running;
        private bool _stop;
        private Task _task;
        private DateTime _next;
        private object _lock = new object();

        public void Recur(TimeSpan cycle, Action<T> action, T thing)
        {
            _cycle = cycle;
            _thing = thing;
            _action = action;
            _running = true;
            _stop = false;
            _next = DateTime.UtcNow;
            _task = Task.Factory.StartNew(RunRecurrence);
        }

        private void RunRecurrence()
        {
            bool isStopping = false;
            lock (_lock) isStopping = _stop;

            while (!isStopping)
            {
                bool isRunning = false;
                lock (_lock) isRunning = _running;
                while (!isRunning)
                {
                    System.Threading.Thread.Sleep(50);
                    lock (_lock) isRunning = _running;
                }

                if (DateTime.UtcNow < _next)
                {
                    var wait = (_next - DateTime.UtcNow).Milliseconds;
                    if (wait < 100) wait = 100;

                    System.Threading.Thread.Sleep(wait);
                }

                if (DateTime.UtcNow >= _next)
                {
                    _action(_thing);
                    _next = DateTime.UtcNow + _cycle;
                }

                lock (_lock) isStopping = _stop;
            }
        }

        public void Toggle()
        {
            lock(_lock)
                _running = !_running;
        }

        public void Stop()
        {
            lock(_lock)
                _stop = true;
            _task.Wait();
        }
    }
}
