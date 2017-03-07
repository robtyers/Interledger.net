using System.Collections.Generic;

namespace Interledger.Net.ILP.Routing.Models
{
    public class Map : Dictionary<string, IRoute>
    {
        public Map() : base() { }
        public Map(int capacity) : base(capacity) { }
    }
}
