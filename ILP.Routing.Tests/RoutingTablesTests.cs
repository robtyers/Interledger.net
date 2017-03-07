using System;
using System.Collections.Generic;
using System.Linq;
using Interledger.Net.ILP.Routing;
using Interledger.Net.ILP.Routing.Models;
using NUnit.Framework;

namespace ILP.Routing.Tests
{
    [TestFixture]
    public class RoutingTablesTests
    {
        private const string LedgerA = "ledgerA.";
        private const string LedgerB = "ledgerB.";
        private const string LedgerC = "ledgerC.";
        private const string LedgerD = "ledgerD.";
        private const string LedgerE = "ledgerE.";

        // connector users
        private string _markA = LedgerA + "mark";
        private string _markB = LedgerB + "mark";

        private RoutingTables _tables;

        private void InitialiseTables()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var route1 = new Route(liquidityCurve1, new[] { LedgerA, LedgerB },
                new RouteInfo()
                    { MinMessageWindow = 1, SourceAccount = _markA, DestinationAccount = _markB,
                    AdditionalInfo = new { rate_info = 0.5D}});

            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } });
            var route2 = new Route(liquidityCurve2, new[] { LedgerB, LedgerA },
                new RouteInfo()
                    { MinMessageWindow = 1, SourceAccount = _markB, DestinationAccount = _markA,
                    AdditionalInfo = new { rate_info = 2.0D }});

            _tables = new RoutingTables(new List<IRoute>() { route1, route2 }, new TimeSpan(0, 0, 0, 45));
        }

        [Test(Description = "routes between multiple local pairs, but doesn't combine them")]
        public void AddLocalRoutes()
        {
            // Arrange
            InitialiseTables();

            var route1 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }), 
                new[] { LedgerB, LedgerC },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = LedgerB + "mark" //"markB"
                });

            var route2 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }),
                new[] { LedgerC, LedgerD },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = LedgerB + "mark" //"markB"
                });

            var route3 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }),
                new[] { LedgerD, LedgerE },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = LedgerD + "mary"
                });
            
            _tables.AddLocalRoutes(new List<IRoute>() { route1, route2, route3 });

            // Act
            // A → B → C
            var actualAtoC = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 20);

            // A → B → C → D → E
            // It can't just skip from A→D, because it isn't a pair, even though its components are local.
            var actualAtoE = _tables.FindBestHopForSourceAmount(LedgerA, LedgerE, 20);

            // C → D → E
            var actualCtoE = _tables.FindBestHopForSourceAmount(LedgerC, LedgerE, 20);

            // Assert
            Assert.AreEqual(LedgerB, actualAtoC.DestinationLedger);
            Assert.AreEqual(LedgerB + "mark", actualAtoC.DestinationCreditAccount);
            Assert.AreEqual(20, actualAtoC.FinalAmount);
            Assert.AreEqual(2, actualAtoC.MinMessageWindow);

            Assert.AreEqual(LedgerB, actualAtoE.DestinationLedger);
            Assert.AreEqual(LedgerB + "mark", actualAtoE.DestinationCreditAccount);
            Assert.AreEqual(80, actualAtoE.FinalAmount);
            Assert.AreEqual(4, actualAtoE.MinMessageWindow);

            Assert.AreEqual(LedgerD, actualCtoE.DestinationLedger);
            Assert.AreEqual(LedgerD + "mary", actualCtoE.DestinationCreditAccount);
            Assert.AreEqual(80, actualCtoE.FinalAmount);
            Assert.AreEqual(2, actualCtoE.MinMessageWindow);
        }

        [Test(Description = "doesn't create a route from A→B→A")]
        public void AddRouteABA()
        {
            // Arrange
            InitialiseTables();

            var route1 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }),
                new[] { LedgerB, LedgerC },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = "markB"
                });

            var route2 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }),
                new[] { LedgerC, LedgerD },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = "markB"
                });

            var route3 = new Route(new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 200D } }),
                new[] { LedgerD, LedgerE },
                new RouteInfo()
                {
                    MinMessageWindow = 1,
                    SourceAccount = LedgerD + "mary"
                });

            // Act
            _tables.AddLocalRoutes(new List<IRoute>() { route1, route2, route3 });

            //Assert
            Assert.IsNull(_tables.Sources.Get(LedgerA).Destinations.Get(LedgerA));
        }

        [Test(Description = "doesn't create a route from A→C→B if A→C isnt local")]
        public void AddRouteACBReturnsNull()
        {
            // Arrange
            InitialiseTables();

            // Implicitly create A→C
            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] {LedgerB, LedgerC};
            var routeInfo = new RouteInfo() {SourceAccount = LedgerB + "mary", MinMessageWindow = 1};
            var route = new Route(liquidityCurve, hops, routeInfo);

            Assert.IsTrue(_tables.AddRoute(route));

            // This should *not* create A→C→B, because A→C isn't local.
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var hops1 = new[] { LedgerC, LedgerB };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerC + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);

            Assert.IsFalse(_tables.AddRoute(route1));

            IRoute nullRoute = null;
            _tables.Sources.Get(LedgerA).Destinations.Get(LedgerB).TryGetValue(LedgerC + "Mary", out nullRoute);
            Assert.IsNull(nullRoute);
        }

        [Test(Description = "creates a route with a custom prefix, if supplied")]
        public void AddRouteCustomPrefix()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] { LedgerB, LedgerC };
            var routeInfo = new RouteInfo() {SourceAccount = LedgerC + "mary", MinMessageWindow = 1, TargetPrefix = "prefix."};
            var route = new Route(liquidityCurve, hops, routeInfo);

            Assert.IsTrue(_tables.AddRoute(route));

            var result = _tables.Sources.Get(LedgerA).Destinations.Get("prefix.")[LedgerC + "mary"];
            Assert.AreEqual(LedgerC, result.DestinationLedger);
            Assert.AreEqual("prefix.", result.TargetPrefix);
        }

        [Test(Description = "doesn't override local pair paths with a remote one")]
        public void AddRouteNotOverrideLocalWithRemote()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 9999D } });
            var hops = new[] { LedgerA, LedgerB };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerA + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);

            // Act
            Assert.IsFalse(_tables.AddRoute(route));

            // Assert
            IRoute nullRoute = null;
            _tables.Sources.Get(LedgerA).Destinations.Get(LedgerB).TryGetValue(LedgerA + "Mary", out nullRoute);
            Assert.IsNull(nullRoute);
        }

        [Test(Description = "doesn't override simple local path A→B with A→C→B")]
        public void AddRouteNotOverrideSimpleLocal()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 999D } });
            var hops = new[] { LedgerA, LedgerC };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerA + "mark", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 999D } });
            var hops1 = new[] { LedgerC, LedgerB };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerC + "mark", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);

            // Act
            _tables.AddLocalRoutes(new List<IRoute>() { route, route1 });

            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 100);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(50D, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 200);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(100D, hop.BestValue);
        }

        [Test(Description = "finds the best next hop when there is one route")]
        public void BestHopWhenOneRouteForSourceAmount()
        {
            // Arrange
            InitialiseTables();
 
            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 0);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(0, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 100);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(50, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 200);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(100, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerB, 300);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(100, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerB, LedgerA, 100);
            Assert.AreEqual(_markA, hop.BestHop);
            Assert.AreEqual(200, hop.BestValue);
        }

        [Test(Description = "finds the best next hop when there are multiple hops")]
        public void NextBestHopMultipleHops()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var hops = new[] { LedgerB, LedgerC };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);

            // Act
            _tables.AddRoute(route);
            
            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "mary", hop.BestHop);
            Assert.AreEqual(25, hop.BestValue);
        }

        [Test(Description = "finds the best next hop when there are multiple routes")]
        public void NextBestHopMultipleRoutes()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] { LedgerB, LedgerC };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);
            _tables.AddRoute(route);

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "martin", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            _tables.AddRoute(route1);
            
            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "mary", hop.BestHop);
            Assert.AreEqual(60D, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 150);
            Assert.AreEqual(LedgerB + "martin", hop.BestHop);
            Assert.AreEqual(75D, hop.BestValue);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 200);
            Assert.AreEqual(LedgerB + "martin", hop.BestHop);
            Assert.AreEqual(100D, hop.BestValue);
        }

        [Test(Description = "finds the best next hop when there is one route")]
        public void BestHopForDestinationAmount()
        {
            // Arrange
            InitialiseTables();


            // Assert
            var hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerB, 0);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(0, hop.BestCost);

            hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerB, 50);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(100, hop.BestCost);

            hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerB, 100);
            Assert.AreEqual(_markB, hop.BestHop);
            Assert.AreEqual(200, hop.BestCost);

            hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerB, 150);
            Assert.IsNull(hop); // ToDo: Should be _markB?
            Assert.IsNull(hop?.BestCost);

            hop = _tables.FindBestHopForDestinationAmount(LedgerB, LedgerA, 200);
            Assert.AreEqual(_markA, hop.BestHop);
            Assert.AreEqual(100, hop.BestCost);
        }

        [Test(Description = "expires old routes")]
        public void ExpireOldRoutes()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] { LedgerB, LedgerC };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);

            // Act
            _tables.AddRoute(route);

            // Assert
            Assert.AreEqual(3, CountRoutes(_tables));

            _tables.RemoveExpiredRoutes();
            Assert.AreEqual(3, CountRoutes(_tables));

            _tables.RemoveExpiredRoutes(DateTime.Now.AddMinutes(10));
            Assert.AreEqual(2, CountRoutes(_tables));
        }

        [Test(Description = "resets expiration to a time in the future")]
        public void BumpConnector()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] { LedgerB, LedgerC };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);

            // Act
            _tables.AddRoute(route);

            // Assert
            Assert.AreEqual(3, CountRoutes(_tables));

            _tables.RemoveExpiredRoutes();
            Assert.AreEqual(3, CountRoutes(_tables));

            _tables.BumpConnector(LedgerB + "mary", new TimeSpan(0, 10, 0));
            Assert.AreEqual(3, CountRoutes(_tables));
        }

        [Test(Description = "removes routes with a nextHop depending on a given connector")]
        public void InvalidateConnector()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            _tables.AddRoute(route1);

            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var hops2 = new[] { LedgerB, LedgerC };
            var routeInfo2 = new RouteInfo() { SourceAccount = LedgerB + "martin", MinMessageWindow = 1 };
            var route2 = new Route(liquidityCurve2, hops2, routeInfo2);
            _tables.AddRoute(route2);

            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "mary", hop.BestHop);
            Assert.AreEqual(60, hop.BestValue);

            _tables.InvalidateConnector(LedgerB + "mary");

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "martin", hop.BestHop);

            var result =_tables.InvalidateConnector(LedgerB + "martin");
            Assert.AreEqual(new [] { LedgerC }, result);
        }

        [Test(Description = "removes routes to a specific ledger, with a nextHop depending on a given connector")]
        public void InvalidateConnectorsRoutesTo()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            _tables.AddRoute(route1);

            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops2 = new[] { LedgerB, LedgerD };
            var routeInfo2 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route2 = new Route(liquidityCurve2, hops2, routeInfo2);
            _tables.AddRoute(route2);

            var liquidityCurve3 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var hops3 = new[] { LedgerB, LedgerC };
            var routeInfo3 = new RouteInfo() { SourceAccount = LedgerB + "martin", MinMessageWindow = 1 };
            var route3 = new Route(liquidityCurve3, hops3, routeInfo3);
            _tables.AddRoute(route3);

            // Assert
            var hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "mary", hop.BestHop);
            Assert.AreEqual(60, hop.BestValue);

            _tables.InvalidateConnectorsRoutesTo(LedgerB + "mary", LedgerD);

            hop = _tables.FindBestHopForSourceAmount(LedgerA, LedgerC, 100);
            Assert.AreEqual(LedgerB + "mary", hop.BestHop);
            Assert.AreEqual(60, hop.BestValue);
        }

        [Test(Description = "removes all of a ledger's routes")]
        public void RemoveRoutesForLedger()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            
            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops2 = new[] { LedgerA, LedgerC };
            var routeInfo2 = new RouteInfo() { SourceAccount = LedgerA + "mary", MinMessageWindow = 1 };
            var route2 = new Route(liquidityCurve2, hops2, routeInfo2);
            
            var liquidityCurve3 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var hops3 = new[] { LedgerC, LedgerA };
            var routeInfo3 = new RouteInfo() { SourceAccount = LedgerC + "mary", MinMessageWindow = 1 };
            var route3 = new Route(liquidityCurve3, hops3, routeInfo3);
            
            var liquidityCurve4 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 100D, 100D } });
            var hops4 = new[] { LedgerC, LedgerB };
            var routeInfo4 = new RouteInfo() { SourceAccount = LedgerC + "mary", MinMessageWindow = 1 };
            var route4 = new Route(liquidityCurve4, hops4, routeInfo4);
            
            _tables.AddLocalRoutes(new List<IRoute>() {route1,route2,route3,route4});

            // Assert
            Assert.AreEqual(6, CountRoutes(_tables));

            // remove the new route
            _tables.RemoveLedger(LedgerC);
            Assert.AreEqual(2, CountRoutes(_tables));
        }

        [Test(Description = "removes no other ledger's routes")]
        public void DoesNotRemoveOtherLedgerRoutes()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var hops = new[] { LedgerC, LedgerA };
            var routeInfo = new RouteInfo() { SourceAccount = LedgerC + "mary", MinMessageWindow = 1 };
            var route = new Route(liquidityCurve, hops, routeInfo);
            _tables.AddLocalRoutes(new List<IRoute>() { route });

            // Assert
            Assert.AreEqual(3, CountRoutes(_tables));

            // remove nonexistent ledger
            _tables.RemoveLedger(LedgerC);
            Assert.AreEqual(2, CountRoutes(_tables));
        }

        [Test(Description = "finds the best route when there is one path")]
        public void FindBestHopForDestinationAmountOnePath()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            _tables.AddRoute(route1);

            // Act
            var hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerC, 25D);

            // Assert
            Assert.AreEqual(false, hop.IsFinal);
            Assert.AreEqual(false, hop.IsLocal);
            Assert.AreEqual(LedgerA, hop.SourceLedger);
            Assert.AreEqual(100D, hop.SourceAmount);
            Assert.AreEqual(LedgerB, hop.DestinationLedger);
            Assert.AreEqual(50D, hop.DestinationAmount);
            Assert.AreEqual(LedgerB + "mary", hop.DestinationCreditAccount);
            Assert.AreEqual(LedgerC, hop.FinalLedger);
            Assert.AreEqual(25D, hop.FinalAmount);
            Assert.AreEqual(2, hop.MinMessageWindow);

            var curve = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 50D}});
            Assert.AreEqual(curve.GetPoints, hop.BestRoute.Curve.GetPoints);
        }

        [Test(Description = "finds the best route when there is one hop")]
        public void FindBestHopForDestinationAmountOneHop()
        {
            // Arrange
            InitialiseTables();

            // Act
            var hop = _tables.FindBestHopForDestinationAmount(LedgerA + "alice", LedgerB + "bob", 50D);

            // Assert
            Assert.AreEqual(true, hop.IsFinal);
            Assert.AreEqual(true, hop.IsLocal);
            Assert.AreEqual(LedgerA, hop.SourceLedger);
            Assert.AreEqual(100D, hop.SourceAmount);
            Assert.AreEqual(LedgerB, hop.DestinationLedger);
            Assert.AreEqual(50D, hop.DestinationAmount);
            Assert.AreEqual(null, hop.DestinationCreditAccount);
            Assert.AreEqual(LedgerB, hop.FinalLedger);
            Assert.AreEqual(50D, hop.FinalAmount);
            Assert.AreEqual(1, hop.MinMessageWindow);
            Assert.AreEqual(new { rate_info = 0.5D }, hop.AdditionalInfo);

            var curve = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 100D}});
            Assert.AreEqual(curve.GetPoints, hop.BestRoute.Curve.GetPoints);
        }

        [Test(Description = "finds the best route when there is a remote path")]
        public void FindBestRouteWhenIsRemotePath()
        {
            // Arrange
            InitialiseTables();

            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var hops1 = new[] { LedgerB, LedgerC };
            var routeInfo1 = new RouteInfo() { SourceAccount = LedgerB + "mary", MinMessageWindow = 1 };
            var route1 = new Route(liquidityCurve1, hops1, routeInfo1);
            _tables.AddRoute(route1);

            // Act
            var hop = _tables.FindBestHopForDestinationAmount(LedgerA, LedgerC + "subledger1.bob", 25D);

            // Assert
            Assert.AreEqual(false, hop.IsFinal);
            Assert.AreEqual(false, hop.IsLocal);
            Assert.AreEqual(LedgerA, hop.SourceLedger);
            Assert.AreEqual(100D, hop.SourceAmount);
            Assert.AreEqual(LedgerB, hop.DestinationLedger);
            Assert.AreEqual(50D, hop.DestinationAmount);
            Assert.AreEqual(LedgerB + "mary", hop.DestinationCreditAccount);
            Assert.AreEqual(LedgerC, hop.FinalLedger);
            Assert.AreEqual(25D, hop.FinalAmount);
            Assert.AreEqual(2, hop.MinMessageWindow);

            var curve = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 50D}});
            Assert.AreEqual(curve.GetPoints, hop.BestRoute.Curve.GetPoints);
        }

        [Test(Description = "finds the best route when there is one hop")]
        public void FindBestHopForSourceAmount()
        {
            // Arrange
            InitialiseTables();
            
            // Act
            var hop = _tables.FindBestHopForDestinationAmount(LedgerA + "Alice", LedgerB + "bob", 50D);

            // Assert
            Assert.AreEqual(true, hop.IsFinal);
            Assert.AreEqual(true, hop.IsLocal);
            Assert.AreEqual(LedgerA, hop.SourceLedger);
            Assert.AreEqual(100D, hop.SourceAmount);
            Assert.AreEqual(LedgerB, hop.DestinationLedger);
            Assert.AreEqual(50D, hop.DestinationAmount);
            Assert.AreEqual(null, hop.DestinationCreditAccount);
            Assert.AreEqual(LedgerB, hop.FinalLedger);
            Assert.AreEqual(50D, hop.FinalAmount);
            Assert.AreEqual(1, hop.MinMessageWindow);
            Assert.AreEqual(new { rate_info = 0.5D }, hop.AdditionalInfo);

            var curve = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 100D}});
            Assert.AreEqual(curve.GetPoints, hop.BestRoute.Curve.GetPoints);
        }

        private static int CountRoutes(RoutingTables tables)
        {
            return tables.Sources.Sources
                .Sum(source => source.Value.Destinations.Size);
        }
    }
}
