using System;
using Interledger.Net.ILP.Routing;
using Interledger.Net.ILP.Routing.Models;
using NUnit.Framework;

namespace ILP.Routing.Tests
{
    [TestFixture(Description = "Route")]
    public class RouteTests
    {
        private const string LedgerA = "ledgerA.";
        private const string LedgerB = "ledgerB.";
        private const string LedgerC = "ledgerC.";
        private const string LedgerD = "ledgerD.";
        private readonly string[] _hopsABC = {LedgerA, LedgerB, LedgerC};
        private readonly string[] _hopsADC = {LedgerA, LedgerD, LedgerC};
        private readonly string[] _hopsBCD = {LedgerB, LedgerC, LedgerD};
        private const string MarkA = LedgerA + "mark";
        private const string MarkC = LedgerC + "mark";

        [Test(Description = "sets up a curve and the hops")]
        public void SetupRoute()
        {
            var liquidityCurve = new LiquidityCurve(new[] {new[] {10D, 20D}, new[] {100D, 200D}});

            var info = new RouteInfo
            {
                MinMessageWindow = 3,
                ExpiresAt = new DateTime(2015, 06, 16, 0, 0, 5),
                IsLocal = true,
                SourceAccount = MarkA,
                DestinationAccount = MarkC,
                AdditionalInfo = new {foo = "bar"}
            };

            var route = new Route(liquidityCurve, _hopsABC, info);

            Assert.AreEqual(route.Hops, _hopsABC);
            Assert.AreEqual(route.SourceLedger, LedgerA);
            Assert.AreEqual(route.NextLedger, LedgerB);
            Assert.AreEqual(route.DestinationLedger, LedgerC);
            Assert.AreEqual(route.TargetPrefix, route.DestinationLedger,
                "should default target prefix to destination ledger");
            Assert.AreEqual(route.MinMessageWindow, 3);
            Assert.AreEqual(route.ExpiresAt, new DateTime(2015, 06, 16, 0, 0, 5));
            Assert.AreEqual(route.IsLocal, true);
            Assert.AreEqual(route.SourceAccount, MarkA);
            Assert.AreEqual(route.DestinationAccount, MarkC);
            Assert.AreEqual(route.AdditionalInfo, new {foo = "bar"});
        }

        [Test(Description = "finds the corresponding amount")]
        public void LiquidityCurveAmountAt()
        {
            var liquidityCurve = new LiquidityCurve(new[] {new[] {10D, 20D}, new[] {100D, 200D}});
            var route = new Route(liquidityCurve, _hopsABC, new RouteInfo());

            Assert.AreEqual(route.AmountAt(55), 110);
        }

        [Test(Description = "finds the corresponding amount")]
        public void LiquidityCurveAmountReverse()
        {
            var liquidityCurve = new LiquidityCurve(new[] {new[] {10D, 20D}, new[] {100D, 200D}});
            var route = new Route(liquidityCurve, _hopsABC, new RouteInfo());

            Assert.AreEqual(route.AmountReverse(110), 55);
        }

        [Test(Description = "finds the corresponding amount")]
        public void LiquidityCurveGetPoints()
        {
            var liquidityCurve = new LiquidityCurve(new[] {new[] {10D, 20D}, new[] {100D, 200D}});
            var route = new Route(liquidityCurve, _hopsABC, new RouteInfo());

            Assert.AreEqual(route.GetPoints, new[] {new[] {10D, 20D}, new[] {100D, 200D}});
        }

        [Test(Description = "combines the curves")]
        public void Combine()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {100D, 100D}});
            var route1 = new Route(liquidityCurve1, _hopsABC, new RouteInfo {MinMessageWindow = 1});
            var liquidityCurve2 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {50D, 60D}});
            var route2 = new Route(liquidityCurve2, _hopsADC, new RouteInfo {MinMessageWindow = 2});

            var combinedRoute = route1.Combine(route2);

            Assert.AreEqual(combinedRoute.GetPoints,
                new[] {new[] {0D, 0D}, new[] {50D, 60D}, new[] {60D, 60D}, new[] {100D, 100D}});
        }

        [Test(Description = "only uses the boundary ledgers in 'hops'")]
        public void CombineUsesBoundaryLedgers()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {100D, 100D}});
            var route1 = new Route(liquidityCurve1, _hopsABC, new RouteInfo {MinMessageWindow = 1});
            var liquidityCurve2 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {50D, 60D}});
            var route2 = new Route(liquidityCurve2, _hopsADC, new RouteInfo {MinMessageWindow = 2});

            var combinedRoute = route1.Combine(route2);

            Assert.AreEqual(combinedRoute.Hops, new[] {LedgerA, LedgerC});
        }

        [Test(Description = "picks the larger minMessageWindow")]
        public void CombinePickLargerMinMessageWindow()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {100D, 100D}});
            var route1 = new Route(liquidityCurve1, _hopsABC, new RouteInfo {MinMessageWindow = 1});
            var liquidityCurve2 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {50D, 60D}});
            var route2 = new Route(liquidityCurve2, _hopsADC, new RouteInfo {MinMessageWindow = 2});

            var combinedRoute = route1.Combine(route2);

            Assert.AreEqual(combinedRoute.MinMessageWindow, 2);
        }

        [Test(Description = "succeeds if the routes are adjacent")]
        public void Join()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 100D}});
            var route1 = new Route(liquidityCurve1, new[] {LedgerA, LedgerB}, new RouteInfo
            {
                IsLocal = true,
                MinMessageWindow = 1
            });

            var liquidityCurve2 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {50D, 60D}});
            var route2 = new Route(liquidityCurve2, _hopsBCD, new RouteInfo
            {
                IsLocal = false,
                MinMessageWindow = 2
            });

            var joinedRoute = route1.Join(route2, new TimeSpan(0, 0, 0, 1));

            // It joins the curves
            Assert.AreEqual(joinedRoute.GetPoints, new[] {new[] {0D, 0D}, new[] {100D, 60D}, new[] {200D, 60D}});

            // It concatenates the hops
            Assert.AreEqual(joinedRoute.Hops, new[] {LedgerA, LedgerB, LedgerC, LedgerD});

            // It isn't a local pair.
            Assert.AreEqual(joinedRoute.IsLocal, false);

            // It combines the minMessageWindows
            Assert.AreEqual(joinedRoute.MinMessageWindow, 3);

            // It sets an expiry in the future
            Assert.IsTrue(DateTime.Now < joinedRoute.ExpiresAt);
        }

        [Test(Description = "sets isLocal to true if both routes are local")]
        public void JoinLocalRoutes()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {200D, 100D}});
            var route1 = new Route(liquidityCurve1, new[] {LedgerA, LedgerB}, new RouteInfo
            {
                IsLocal = true,
                MinMessageWindow = 1
            });

            var liquidityCurve2 = new LiquidityCurve(new[] {new[] {0D, 0D}, new[] {50D, 60D}});
            var route2 = new Route(liquidityCurve2, _hopsBCD, new RouteInfo
            {
                IsLocal = true,
                MinMessageWindow = 1
            });

            var joinedRoute = route1.Join(route2, new TimeSpan(0, 0, 0, 1));

            // It is a local pair.
            Assert.IsTrue(joinedRoute.IsLocal);
        }


        [Test(Description = "fails if the routes aren't adjacent")]
        public void FailIfUnadjacent()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var route1 = new Route(liquidityCurve1, new[] { LedgerA, LedgerB }, new RouteInfo());

            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var route2 = new Route(liquidityCurve2, new[] { LedgerC, LedgerD }, new RouteInfo());

            Assert.Throws<ArgumentException>(() => route1.Join(route2, new TimeSpan(0, 0, 0, 0)), $"{route2.SourceLedger} is not adjacent.");
        }

        [Test(Description = "fails if the joined route would double back")]
        public void FailIfDoublesBack()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var route1 = new Route(liquidityCurve1, new[] { LedgerB, LedgerA }, new RouteInfo());

            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D } });
            var route2 = new Route(liquidityCurve2, _hopsABC, new RouteInfo());

            Assert.IsNull(route1.Join(route2, new TimeSpan(0, 0, 0, 0)));
        }

        [Test(Description = "creates a shifted route")]
        public void ShiftY()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 50D, 60D }, new[] { 100D, 100D } });
            var route1 = new Route(liquidityCurve1, new[] {LedgerB, LedgerA}, new RouteInfo {IsLocal = true});

            var route2 = route1.ShiftY(1);

            Assert.IsTrue(route2.IsLocal);
            Assert.AreEqual(route2.Curve.Points, new[] { new[] { 0D, 1D }, new[] { 50D, 61D }, new[] { 100D, 101D } });
        }

        [Test(Description = "doesn't expire routes by default")]
        public void IsExpired()
        {
            var liquidityCurve1 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var route1 = new Route(liquidityCurve1, new[] { LedgerB, LedgerA }, new RouteInfo());

            var now = new DateTime(2016, 06, 16, 13, 00, 00);
            var liquidityCurve2 = new LiquidityCurve(new[] { new[] { 0D, 0D }, new[] { 200D, 100D } });
            var route2 = new Route(liquidityCurve2, new[] { LedgerB, LedgerA }, new RouteInfo {ExpiresAt = now.AddSeconds(1)});

            Assert.IsFalse(route1.IsExpired(now));
            Assert.IsFalse(route2.IsExpired(now));

            Assert.IsFalse(route1.IsExpired(now.AddSeconds(5)));
            Assert.IsTrue(route2.IsExpired(now.AddSeconds(5)));
        }
    }
}
