using Interledger.Net.ILP.Routing.Models;

namespace Interledger.Net.ILP.Routing
{
    public interface IRoutingTable
    {
        void AddRoute(string destination, string nextHop, IRoute route);
        bool RemoveRoute(string destination, string nextHop);
        Hop FindBestHopForSourceAmount(string destination, double sourceAmount);
        Hop FindBestHopForDestinationAmount(string destination, double destinationAmount);
    }
}