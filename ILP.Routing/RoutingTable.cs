using System.Linq;
using Interledger.Net.ILP.Routing.Models;

namespace Interledger.Net.ILP.Routing
{
    public class RoutingTable : IRoutingTable
    {
        // nextHop and bestHop are IlpAddress's referring to the connector's account
        // on the source ledger.

        public readonly PrefixMap<Map> Destinations;

        public RoutingTable()
        {
            Destinations = new PrefixMap<Map>();
        }

        public void AddRoute(string destination, string nextHop, IRoute route)
        {
            var routes = Destinations.Get(destination);

            if (routes == null)
            {
                routes = new Map();
                Destinations.Insert(destination, routes);
            }
                
            routes.Add(nextHop, route);//ToDo: duplicate keys?
        }

        public bool RemoveRoute(string destination, string nextHop)
        {
            var routes = Destinations.Get(destination);

            if (routes == null)
                return false;

            routes.Remove(nextHop);

            if (routes.Any())
                return false;

            Destinations.Delete(destination);
            return true;
        }

        public Hop FindBestHopForSourceAmount(string destination, double sourceAmount)
        {
            var routes = Destinations.Resolve(destination);

            var bestHop = routes?
                .OrderByDescending(d => d.Value.AmountAt(sourceAmount))
                .Select(h => new Hop
                {
                    BestHop = h.Key,
                    BestValue = h.Value.AmountAt(sourceAmount),
                    BestRoute = h.Value
                })
                .FirstOrDefault();

            return bestHop;
        }

        public Hop FindBestHopForDestinationAmount(string destination, double destinationAmount)
        {
            var routes = Destinations.Resolve(destination);

            var bestHop = routes?
                .Where(d => !double.IsPositiveInfinity(d.Value.AmountReverse(destinationAmount)))
                .OrderBy(d => d.Value.AmountReverse(destinationAmount))
                .Select(h => new Hop
                {
                    BestHop = h.Key,
                    BestCost = h.Value.AmountReverse(destinationAmount),
                    BestRoute = h.Value
                })
                .FirstOrDefault();

            return bestHop;
        }
    }
}
