using System;
using System.Collections.Generic;
using System.Linq;
using Interledger.Net.ILP.Routing.Models;

namespace Interledger.Net.ILP.Routing
{
    public class RoutingTables
    {
        private static TimeSpan _expiryDuration;

        /// <summary>
        /// A next hop of PAIR distinguishes a local pair A→B from a complex route
        /// that just happens to be local, i.e. when A→C & C→B are local pairs.
        /// </summary>
        private const string Pair = "PAIR";

        public readonly IPrefixMap<RoutingTable> Sources;
        private readonly Dictionary<string, string> _localAccounts;

        //public RoutingTables(List<RouteData> localRoutes, int expiryDuration)
        //{
        //    throw new NotImplementedException();
        //}

        public RoutingTables(List<IRoute> localRoutes, TimeSpan expiryDuration)
        {
            _expiryDuration = expiryDuration;
            Sources = new PrefixMap<RoutingTable>(); // { "sourceLedger" => RoutingTable }
            _localAccounts = new Dictionary<string, string>(); // { "ledger" ⇒ accountURI }

            AddLocalRoutes(localRoutes);
        }

        /// <summary>
        /// @param {RouteData[]|Route[]} localRoutes - Each local route should include the optional
        /// `destinationAccount` parameter.
        /// </summary>
        /// <param name="localRoutes"></param>
        public void AddLocalRoutes(List<IRoute> localRoutes)
        {
            foreach (var localRoute in localRoutes)
            {
                //localRoute.ExpiresAt = (localRoute.ExpiresAt ?? DateTime.Now).Add(_expiryDuration);
                localRoute.IsLocal = true;
                var table =
                    Sources.Get(localRoute.SourceLedger) ??
                    Sources.Insert(localRoute.SourceLedger, new RoutingTable());

                table.AddRoute(localRoute.DestinationLedger, Pair, localRoute);

                _localAccounts[localRoute.SourceLedger] = localRoute.SourceAccount;

                if (!string.IsNullOrEmpty(localRoute.DestinationAccount))
                {
                    _localAccounts[localRoute.DestinationLedger] = localRoute.DestinationAccount;
                }
            }

            localRoutes.ForEach((route) => AddRoute(route));
        }

        public void RemoveLedger(string ledger)
        {
            foreach (var prefixMap in Sources.Sources)
            {
                var ledgerA = prefixMap.Key;
                var tableFromA = prefixMap.Value;
                foreach (var d in tableFromA.Destinations.Sources.ToList())
                {
                    var ledgerB = d.Key;

                    if (ledgerA == ledger || ledgerB == ledger)
                    {
                        var routesFromAtoB = d.Value;
                        foreach (var nextHop in routesFromAtoB.Keys.ToList())
                        {
                            RemoveRoute(ledgerA, ledgerB, nextHop);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a `route` B→C, create a route A→C for each source ledger A with a
        /// local route to B.
        /// 
        /// @param {Route|RouteData} _route from ledger B→C
        /// @returns {Boolean} whether or not a new route was added
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public bool AddRoute(IRoute route)
        {
            var added = false;

            foreach (var sourcesPrefix in Sources.Prefixes)
            {
                if (AddRouteFromSource(Sources.Sources[sourcesPrefix], sourcesPrefix, route))
                    added = true;
            }

            return added;
        }

        private bool AddRouteFromSource(IRoutingTable tableFromA, string ledgerA, IRoute routeFromBtoC)
        {
            var ledgerB = routeFromBtoC.SourceLedger;
            var ledgerC = routeFromBtoC.TargetPrefix;
            var connectorFromBtoC = routeFromBtoC.SourceAccount;
            var added = false;

            // Don't create local route A→B→C if local route A→C already exists.
            if (routeFromBtoC.IsLocal && GetLocalPairRoute(ledgerA, ledgerC) != null)
                return false;

            // Don't create A→B→C when A→B is not a local pair.
            var routeFromAtoB = GetLocalPairRoute(ledgerA, ledgerB);

            // Make sure the routes can be joined.
            var routeFromAtoC = routeFromAtoB?.Join(routeFromBtoC, _expiryDuration);
            if (routeFromAtoC == null)
                return false;

            if (GetRoute(ledgerA, ledgerC, connectorFromBtoC) == null)
                added = true;

            tableFromA.AddRoute(ledgerC, connectorFromBtoC, routeFromAtoC);

            // Given pairs A↔B,B→C; on addRoute(C→D) create A→D after creating B→D.
            if (added)
                added = AddRoute(routeFromAtoC) || added; // ToDo: check added logic

            return added;
        }

        private void RemoveRoute(string ledgerB, string ledgerC, string connectorFromBtoC)
        {
            foreach (var source in Sources.Sources)
            {
                if (source.Key == ledgerB)
                    source.Value.RemoveRoute(ledgerC, connectorFromBtoC);
            }
        }

        public void RemoveExpiredRoutes()
        {
            RemoveExpiredRoutes(DateTime.Now);
        }

        public void RemoveExpiredRoutes(DateTime now)
        {
            var lostLinks = new List<Tuple<string, string, string>>();

            foreach (var prefixMap in Sources.Sources)
            {
                var ledgerA = prefixMap.Key;
                var tableFromA = prefixMap.Value;
                foreach (var d in tableFromA.Destinations.Sources)
                {
                    var ledgerB = d.Key;
                    var routesFromAtoB = d.Value;
                    foreach (var nextHop in routesFromAtoB.Keys)
                    {
                        var routeFromAtoB = routesFromAtoB[nextHop];
                        if (routeFromAtoB.IsExpired(now))
                            lostLinks.Add(new Tuple<string, string, string>(ledgerA, ledgerB, nextHop));
                    }
                }
            }

            lostLinks.ForEach(link => RemoveRoute(link.Item1, link.Item2, link.Item3));
        }

        private IRoute GetLocalPairRoute(string ledgerA, string ledgerB)
        {
            return GetRoute(ledgerA, ledgerB, Pair);
        }

        private IRoute GetRoute(string ledgerA, string ledgerB, string nextHop)
        {
            IRoute route = null;

            var routesFromAtoB = Sources.Get(ledgerA).Destinations.Get(ledgerB);
            routesFromAtoB?.TryGetValue(nextHop, out route);

            return route;
        }

        /// <summary>
        /// Find the best intermediate ledger (`nextLedger`) to use after `sourceLedger` on
        /// the way to `finalLedger`.
        /// This connector must have `[sourceLedger, nextLedger]` as a pair.
        /// @param {IlpAddress} sourceAddress
        /// @param {IlpAddress} finalAddress
        /// @param {String} finalAmount
        /// @returns {Object}
        /// 
        /// </summary>
        /// <param name="sourceAddress"></param>
        /// <param name="finalAddress"></param>
        /// <param name="finalAmount"></param>
        public Hop FindBestHopForDestinationAmount(string sourceAddress, string finalAddress, double finalAmount)
        {
            var nextHop = FindNextBestHopForDestinationAmount(sourceAddress, finalAddress, Math.Abs(finalAmount));
            if (nextHop == null)
                return null;

            // sourceLedger is the longest known prefix of sourceAddress (likewise for
            // finalLedger/finalAddress).
            var sourceLedger = nextHop.BestRoute.SourceLedger;
            var finalLedger = nextHop.BestRoute.DestinationLedger;
            var nextLedger = nextHop.BestRoute.NextLedger;
            var routeFromAtoB = GetLocalPairRoute(sourceLedger, nextLedger);
            var isFinal = (nextLedger == finalLedger);

            return new Hop()
            {
                IsFinal = isFinal,
                IsLocal = nextHop.BestRoute.IsLocal,
                BestHop = nextHop.BestHop,
                BestCost = nextHop.BestCost,
                BestRoute = nextHop.BestRoute,
                SourceLedger = sourceLedger,
                SourceAmount = nextHop.BestCost,
                DestinationLedger = nextLedger,
                DestinationAmount = routeFromAtoB.AmountAt(nextHop.BestCost),
                DestinationCreditAccount = isFinal ? null : nextHop.BestHop,
                FinalLedger = finalLedger,
                FinalAmount = finalAmount,
                FinalPrecision = nextHop.BestRoute.DestinationPrecision,
                FinalScale = nextHop.BestRoute.DestinationScale,
                MinMessageWindow = nextHop.BestRoute.MinMessageWindow,
                AdditionalInfo = isFinal ? nextHop.BestRoute.AdditionalInfo : null
            };
        }

        ///**
        // * @param {IlpAddress} sourceAddress
        // * @param {IlpAddress} finalAddress
        // * @param {String} sourceAmount
        // * @returns {Object}
        // */
        public Hop FindBestHopForSourceAmount(string sourceAddress, string finalAddress, double sourceAmount)
        {
            var nextHop = FindNextBestHopForSourceAmount(sourceAddress, finalAddress, Math.Abs(sourceAmount));
            if (nextHop == null)
                return null; 
          var
            sourceLedger = nextHop.BestRoute.SourceLedger;
            var finalLedger = nextHop.BestRoute.DestinationLedger;
            var nextLedger = nextHop.BestRoute.NextLedger;
            var routeFromAToB = GetLocalPairRoute(sourceLedger, nextLedger);
            var isFinal = nextLedger == finalLedger;

            return new Hop()
            {
                IsFinal = isFinal,
                IsLocal = nextHop.BestRoute.IsLocal,
                BestHop = nextHop.BestHop,
                BestValue = nextHop.BestValue,
                SourceLedger = sourceLedger,
                SourceAmount = sourceAmount,
                DestinationLedger = nextLedger,
                DestinationAmount = routeFromAToB.AmountAt(Math.Abs(sourceAmount)),
                DestinationCreditAccount = isFinal ? null : nextHop.BestHop,
                FinalLedger = finalLedger,
                FinalAmount = nextHop.BestValue,
                FinalPrecision = nextHop.BestRoute.DestinationPrecision,
                FinalScale = nextHop.BestRoute.DestinationScale,
                MinMessageWindow = nextHop.BestRoute.MinMessageWindow,
                AdditionalInfo = isFinal ? nextHop.BestRoute.AdditionalInfo : null
            };
        }

        private Hop FindNextBestHopForSourceAmount(string source, string destination, double amount)
        {
            
          var table = Sources.Resolve(source);
          if (table == null)
                return null;

            return RewriteLocalHop(table.FindBestHopForSourceAmount(destination, amount));
        }

        private Hop FindNextBestHopForDestinationAmount(string source, string destination, double amount)
        {
        
            var table = Sources.Resolve(source);
            if (table == null)
                return null;
            
            return RewriteLocalHop(table.FindBestHopForDestinationAmount(destination, amount));
        }

        private Hop RewriteLocalHop(Hop hop)
        {
            if (hop != null && hop.BestHop == Pair)
            {
                hop.BestHop = _localAccounts[hop.BestRoute.DestinationLedger];
            }

            return hop;
        }
        
        public void BumpConnector(string connectorAccount, TimeSpan holdDownTime)
        {
            foreach (var prefixMap in Sources.Sources)
            {
                var table = prefixMap.Value;
                foreach (var destination in table.Destinations.Sources)
                {
                    var routesFromAtoB = destination.Value;
                    foreach (var nextHop in routesFromAtoB)
                    {
                        if (connectorAccount == nextHop.Key)
                            nextHop.Value.BumpExpiration(holdDownTime);
                    }
                }
            }
        }

        public List<string> InvalidateConnector(string connectorAccount)
        {

            var lostLedgerLinks = new List<string>();
            
            foreach (var prefixMap in Sources.Sources)
            {
                var table = prefixMap.Value;
                foreach (var destination in table.Destinations.Sources.ToList())
                {
                    if (table.RemoveRoute(destination.Key, connectorAccount))
                    {
                        lostLedgerLinks.Add(destination.Key);
                    }
                }
            }

            return lostLedgerLinks;
        }

        public List<string> InvalidateConnectorsRoutesTo(string connectorAccount, string ledger)
        {
            var lostLedgerLinks = new List<string>();

            foreach (var prefixMap in Sources.Sources)
            {
                var table = prefixMap.Value;
                if (table.RemoveRoute(ledger, connectorAccount))
                {
                    lostLedgerLinks.Add(ledger);
                }
            }
            
            return lostLedgerLinks;
        }

        //  function combineRoutesByConnector(routesByConnector, maxPoints)
        //  {
        //      const routes = routesByConnector.values()
        //    let totalRoute = routes.next().value
        //    for (const subRoute of routes) {
        //          totalRoute = totalRoute.combine(subRoute)
        //}
        //      return totalRoute.simplify(maxPoints)
        //  }
    }
}
