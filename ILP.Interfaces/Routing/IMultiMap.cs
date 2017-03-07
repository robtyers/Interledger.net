using System.Collections.Generic;

namespace Interledger.Net.ILP.Interfaces.Routing
{
    public interface IMultiMap
    {
        void Add(string key, IRoute value);
        IEnumerable<string> Keys { get; }
        List<IRoute> this[string key] { get; }
        void Remove(string key);
    }
}