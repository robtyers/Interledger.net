using System;
using System.Collections.Generic;

namespace Interledger.Net.ILP.Routing
{
    public interface IPrefixMap<T>
    {
        List<string> Prefixes { get; }
        Dictionary<string, T> Sources { get; }
        int Size { get; }
        List<string> Keys { get; }
        T Resolve(string key);
        T Get(string prefix);
        void Each(Func<T, string, bool> fn);
        T Insert(string prefix, T item);
        void Delete(string prefix);
    }
}