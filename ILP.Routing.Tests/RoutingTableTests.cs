using Interledger.Net.ILP.Routing;
using Interledger.Net.ILP.Routing.Models;
using NUnit.Framework;

namespace ILP.Routing.Tests
{
    [TestFixture(Description = "RoutingTable")]
    public class RoutingTableTests
    {
        private readonly IRoute _route;

        private const string LedgerB = "ledgerB.";
        private readonly string _markB = string.Concat(LedgerB, "mark");
        private readonly string _maryB = string.Concat(LedgerB, "mary");
        
        public RoutingTableTests()
        {
            var fakeInfo1 = new RouteInfo() { IsLocal = true };
            ILiquidityCurve fakeCurve1 = new LiquidityCurve();
            IRoute fakeRoute1 = new Route(fakeCurve1, new string[2], fakeInfo1);
            _route = fakeRoute1;
        }

        [Test(Description = "stores a route")]
        public void AddRoute()
        {
            var table = new RoutingTable();
            table.AddRoute(LedgerB, _markB, _route);

            Assert.AreEqual(table.Destinations.Get(LedgerB)[_markB], _route);
        }

        [Test(Description = "removes a route")]
        public void RemoveRoute()
        {
            var table = new RoutingTable();
            table.AddRoute(LedgerB, _markB, _route);

            table.RemoveRoute(LedgerB, _markB);

            Assert.AreEqual(0, table.Destinations.Size);
        }
        
        [Test(Description = "ignores a nonexistant route")]
        public void IgnoreRoute()
        {
            var table = new RoutingTable();
            Assert.DoesNotThrow(() => table.RemoveRoute(LedgerB, _markB));
        }


        [Test(Description = "returns the best hop for source amount")]
        public void BestHopForSourceAmount()
        {
            var table = new RoutingTable();
            var curveMark = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var curveMary = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var routeMark = new Route(curveMark, new string[2], new RouteInfo());
            var routeMary = new Route(curveMary, new string[2], new RouteInfo());

            table.AddRoute(LedgerB, _markB, routeMark);
            table.AddRoute(LedgerB, _maryB, routeMary);

            Assert.AreEqual(_maryB, table.FindBestHopForSourceAmount(LedgerB, 50).BestHop);
            Assert.AreEqual(60, table.FindBestHopForSourceAmount(LedgerB, 50).BestValue);
            Assert.AreEqual(routeMary, table.FindBestHopForSourceAmount(LedgerB, 50).BestRoute);

            Assert.AreEqual(_markB, table.FindBestHopForSourceAmount(LedgerB, 70).BestHop);
            Assert.AreEqual(70, table.FindBestHopForSourceAmount(LedgerB, 70).BestValue);
            Assert.AreEqual(routeMark, table.FindBestHopForSourceAmount(LedgerB, 70).BestRoute);
            
            Assert.AreEqual(_markB, table.FindBestHopForSourceAmount(LedgerB, 200).BestHop);
            Assert.AreEqual(100, table.FindBestHopForSourceAmount(LedgerB, 200).BestValue);
            Assert.AreEqual(routeMark, table.FindBestHopForSourceAmount(LedgerB, 200).BestRoute);
        }


        [Test(Description = "returns undefined when there is no route to the destination")]
        public void ReturnsNullForSourceAmount()
        {
            var table = new RoutingTable();
            Assert.IsNull(table.FindBestHopForSourceAmount(LedgerB, 10));
        }
        
        [Test(Description = "returns the best hop for destination amount")]
        public void BestHopForDestinationAmount()
        {
            var table = new RoutingTable();
            var curveMark = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var curveMary = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var routeMark = new Route(curveMark, new string[2], new RouteInfo());
            var routeMary = new Route(curveMary, new string[2], new RouteInfo());

            table.AddRoute(LedgerB, _markB, routeMark);
            table.AddRoute(LedgerB, _maryB, routeMary);

            Assert.AreEqual(_maryB, table.FindBestHopForDestinationAmount(LedgerB, 60).BestHop);
            Assert.AreEqual(50, table.FindBestHopForDestinationAmount(LedgerB, 60).BestCost);
            Assert.AreEqual(routeMary,table.FindBestHopForDestinationAmount(LedgerB, 60).BestRoute);

            Assert.AreEqual(_markB, table.FindBestHopForDestinationAmount(LedgerB, 70).BestHop);
            Assert.AreEqual(70, table.FindBestHopForDestinationAmount(LedgerB, 70).BestCost);
            Assert.AreEqual(routeMark, table.FindBestHopForDestinationAmount(LedgerB, 70).BestRoute);
        }

        [Test(Description = "returns undefined when there is no route to the destination")]
        public void ReturnsNullForDestinationAmount()
        {
            var table = new RoutingTable();
            Assert.IsNull(table.FindBestHopForDestinationAmount(LedgerB, 10));
        }

        [Test(Description = "returns undefined when no route has a high enough destination amount")]
        public void ReturnsNullForTooLowDestinationAmount()
        {
            var table = new RoutingTable();
            var curveMark = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var routeMark = new Route(curveMark, new string[2], new RouteInfo());
            table.AddRoute(LedgerB, _markB, routeMark);

            Assert.IsNull(table.FindBestHopForDestinationAmount(LedgerB, 200));
        }
    }
}
