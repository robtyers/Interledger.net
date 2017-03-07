using System;
using System.Collections.Generic;
using System.Linq;
using Interledger.Net.ILP.Routing.Models;
using Newtonsoft.Json;

namespace Interledger.Net.ILP.Routing
{
    public class Route : IRoute
    {
        public ILiquidityCurve Curve { get; }
        public string[] Hops { get; }
        public string SourceLedger { get; }
        public string NextLedger { get; }
        public string DestinationLedger { get; }
        public string TargetPrefix { get; }
        public int MinMessageWindow { get; }
        public DateTime? ExpiresAt { get; set; }
        public object AdditionalInfo { get; }
        public bool IsLocal { get; set; }
        public string SourceAccount { get; }
        public string DestinationAccount { get; }
        private readonly RouteInfo _info;

        public Route(ILiquidityCurve curve, string[] hops, RouteInfo info)
        {
            _info = info;
            Hops = hops;
            Curve = curve;

            SourceLedger = hops[0];
            NextLedger = hops[1];
            DestinationLedger = hops[hops.Length - 1];

            // if targetPrefix is specified, then destinations matching 'targetPrefix'
            // will follow this route, rather than destinations matching
            // 'destinationLedger'
            TargetPrefix = string.IsNullOrEmpty(info.TargetPrefix) 
                ? DestinationLedger 
                : info.TargetPrefix;

            MinMessageWindow = info.MinMessageWindow;
            ExpiresAt = info.ExpiresAt;
            AdditionalInfo = info.AdditionalInfo;

            IsLocal = info.IsLocal;
            SourceAccount = info.SourceAccount;
            DestinationAccount = info.DestinationAccount;
        }

        // Proxy some functions to the LiquidityCurve.
        public int DestinationScale { get; set; }
        public double AmountAt(double x) => Curve.AmountAt(x);
        public double AmountReverse(double x) => Curve.AmountReverse(x);
        public double[][] GetPoints => Curve.GetPoints;
        public int DestinationPrecision { get; set; }

        public IRoute Combine(IRoute alternateRoute)
        {
            var combinedCurve = Curve.Combine(alternateRoute.Curve);
            var combinedHops = SimpleHops;

            return new Route(combinedCurve, combinedHops, new RouteInfo
            {
                MinMessageWindow = Math.Max(MinMessageWindow, alternateRoute.MinMessageWindow),
                IsLocal = false
            });
        }

        public IRoute Join(IRoute tailRoute, TimeSpan expiryDuration)
        {
            // Sanity check: make sure the routes are actually adjacent.
            if (DestinationLedger != tailRoute.SourceLedger)
                throw new ArgumentException($"{tailRoute.SourceLedger} is not adjacent.");

            // Don't create A→B→A.
            // In addition, ensure that it doesn't double back, i.e. B→A→B→C.
            if (RouteHelper.Intersect(Hops, tailRoute.Hops) > 1)
                return null;

            var joinedCurve = Curve.Join(tailRoute.Curve);
            var joinedHops = Hops.Concat(tailRoute.Hops.Skip(1)).ToArray();
            return new Route(joinedCurve, joinedHops, new RouteInfo
            {
                MinMessageWindow = MinMessageWindow + tailRoute.MinMessageWindow,
                IsLocal = IsLocal && tailRoute.IsLocal,
                SourceAccount = SourceAccount,
                ExpiresAt = DateTime.Now.Add(expiryDuration),
                TargetPrefix = tailRoute.TargetPrefix
            });
        }

        public IRoute ShiftY(double dy)
        {
            return new Route(Curve.ShiftY(dy), Hops, _info);
        }

        public IRoute Simplify(int maxPoints)
        {
            return new Route(Curve.Simplify(maxPoints), SimpleHops, new RouteInfo
            {
                MinMessageWindow = MinMessageWindow,
                AdditionalInfo = AdditionalInfo,
                IsLocal = IsLocal,
                TargetPrefix = TargetPrefix
            });
        }

        public bool IsExpired(DateTime now) => ExpiresAt != null && ExpiresAt < now;

        public string ToJSON()
        {
            return JsonConvert.SerializeObject(
                new
                {
                    source_ledger = SourceLedger,
                    destination_ledger = DestinationLedger,
                    points = GetPoints,
                    min_message_window = MinMessageWindow,
                    source_account = SourceAccount
                });
        }

        public void BumpExpiration(TimeSpan holdDownTime)
        {
            ExpiresAt = ExpiresAt?.Add(holdDownTime);
        }

        private string[] SimpleHops => new[] { SourceLedger, DestinationLedger };
    }

    public static class RouteHelper
    {
        public static int Intersect<T>(IEnumerable<T> listA, IEnumerable<T> listB)
        {
            return listA
                .Select(i => i)
                .Intersect(listB)
                .Count();
        }
    }
}
