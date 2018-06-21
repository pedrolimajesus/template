using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents
{
    public interface IRecurrence<T>
    {
        void Recur(TimeSpan cycle, Action<T> action, T thing);
        void Toggle();
        void Stop();
    }
}
