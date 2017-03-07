using System.Collections.Generic;

namespace Interledger.Net.ILP.Interfaces.Routing
{
    public interface IPrefixMap
    {
        List<string> Prefixes { get; }
        int Size { get; }
        List<string> Keys { get; }
        IMap Resolve(string key);
        IMap Get(string prefix);
        IMap Insert(string prefix, IMap item);
        void Delete(string prefix);
    }
}