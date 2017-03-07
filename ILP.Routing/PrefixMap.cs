using System;
using System.Collections.Generic;
using System.Linq;

namespace Interledger.Net.ILP.Routing
{
    /// <summary>
    /// A key-value RoutingTable where the members' keys represent prefixes.
    /// 
    /// Example:
    /// const RoutingTable = new PrefixRoutingTable()
    /// RoutingTable.insert("foo", 1)
    /// RoutingTable.insert("bar", 2)
    /// RoutingTable.get("foo")     // ⇒ 1
    /// RoutingTable.get("foo.bar") // ⇒ 1 ("foo" is the longest known prefix of "foo.bar")
    /// RoutingTable.get("bar")     // ⇒ 2
    /// RoutingTable.get("bar.foo") // ⇒ 2 ("bar" is the longest known prefix of "bar.foo")
    /// RoutingTable.get("random")  // ⇒ null
    /// 
    /// </summary>

    public class PrefixMap<T> : IPrefixMap<T> where T : new()
    {

        private readonly List<string> _prefixes;

        public PrefixMap()
        {
            _prefixes = new List<string>();
            Sources = new Dictionary<string, T>();
        }

        public PrefixMap(IEnumerable<string> prefixes, Dictionary<string, T> sources)
        {
            _prefixes = new List<string>(prefixes);
            Sources = sources;
        }

        public Dictionary<string, T> Sources { get; }

        public List<string> Prefixes => _prefixes.OrderBy(prefix => prefix).ToList();
        public int Size => _prefixes.Count;
        public List<string> Keys => _prefixes;

        public T Resolve(string key)
        {
            // Exact match
            if (Sources.ContainsKey(key))
                return Sources[key];

            if (!_prefixes.Any(key.StartsWith))
                return default(T);

            var prefix = _prefixes
                .OrderByDescending(p => p.Length)
                .ThenByDescending(p => p)
                .First(key.StartsWith);

            return Sources[prefix];
        }

        public T Get(string prefix)
        {
            return Sources.ContainsKey(prefix) ? Sources[prefix] : default(T);
        }

        /// <summary>
        /// @param {function(item, key)} fn
        /// </summary>
        /// <param name="fn"></param>
        public void Each(Func<T, string, bool> fn)
        {
            foreach (var prefix in _prefixes)
            {
                fn(Sources[prefix], prefix);
            }
        }
        
        public T Insert(string prefix, T item)
        {
            if (!Sources.ContainsKey(prefix))
            {
                var index = _prefixes
                    .FindIndex(e => prefix.Length > e.Length || string.Compare(prefix, e, StringComparison.Ordinal) > 0);

                if (index < 0)
                {
                    _prefixes.Add(prefix);
                }
                else
                {
                    _prefixes.Splice(index, 0, prefix);
                }

                Sources.Add(prefix, new T());
            }

            Sources[prefix] = item;
            return item;
        }

        public void Delete(string prefix)
        {
            _prefixes.Remove(prefix);
            Sources.Remove(prefix);
        }
    }

    public static class Extensions
    {
        public static List<T> Splice<T>(this List<T> source, int index, int count, T item)
        {
            var items = source.GetRange(index, count);

            source.RemoveRange(index, count);
            source.Add(item);
            source.AddRange(items);

            return source;
        }
    }
}
