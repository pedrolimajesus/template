using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lok.Unik.ModelCommon.Interfaces
{
    public interface ICriteria<E,T>
    {
        List<E> Criteria(List<E> entities, T filters);
    }
}
