using System.Collections.Generic;
using Interledger.Net.ILP.Routing;
using Interledger.Net.ILP.Routing.Models;
using NUnit.Framework;

namespace ILP.Routing.Tests
{
    [TestFixture(Description = "PrefixMap")]
    public class PrefixMapTests
    {
        private readonly Map _fakeItem1;
        private readonly Map _fakeItem2;
        private readonly Map _fakeItemAny;

        public PrefixMapTests()
        {
            var fakeInfo1 = new RouteInfo() {IsLocal = true};
            ILiquidityCurve fakeCurve1 = new LiquidityCurve();
            var fakeRoute1 = new Route(fakeCurve1, new string[2], fakeInfo1);
            _fakeItem1 = new Map() { {"hop", fakeRoute1} };
            
            var fakeInfo2 = new RouteInfo() {IsLocal = false};
            ILiquidityCurve fakeCurve2 = new LiquidityCurve();
            IRoute fakeRoute2 = new Route(fakeCurve2, new string[2], fakeInfo2);
            _fakeItem2 = new Map() { { "hop", fakeRoute2 } };

            _fakeItemAny = new Map() { { "nextHop", fakeRoute1 } };
        }

        [Test(Description = "returns a sorted list of keys")]
        public void Keys()
        {
            var sut = new PrefixMap<Map>();

            Assert.AreEqual(new string[0], sut.Prefixes);

            sut.Insert("foo", _fakeItem1);
            Assert.AreEqual(new[] {"foo"}, sut.Prefixes);

            sut.Insert("bar", _fakeItem1);
            Assert.AreEqual(new[] {"bar", "foo"}, sut.Prefixes);
        }

        [Test(Description = "returns the number of items in the map")]
        public void Size()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);
            Assert.AreEqual(1, sut.Size);
        }

        [Test(Description = "returns an exact match")]
        public void ResolveExact()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);
            sut.Insert("bar", _fakeItem2);

            Assert.AreEqual(_fakeItem1, sut.Resolve("foo"));
            Assert.AreEqual(_fakeItem2, sut.Resolve("bar"));
        }

        [Test(Description = "returns a prefix match")]
        public void ResolvePrefix()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);

            Assert.AreEqual(_fakeItem1, sut.Resolve("foo123"));
            Assert.AreEqual(_fakeItem1, sut.Resolve("foo12"));
            Assert.AreEqual(_fakeItem1, sut.Resolve("foo1"));
        }

        [Test(Description = "returns null for no match")]
        public void ResolveNoMatch()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);

            Assert.IsNull(sut.Resolve("a"));
            Assert.IsNull(sut.Resolve("z"));
        }

        [Test(Description = "supports a catch-all key")]
        public void ResolveCatchAllKey()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("", _fakeItemAny);
            sut.Insert("foo", _fakeItem1);

            Assert.AreEqual(_fakeItem1, sut.Resolve("foo"));
            Assert.AreEqual(_fakeItemAny, sut.Resolve("fo"));
            Assert.AreEqual(_fakeItemAny, sut.Resolve("f"));
            Assert.AreEqual(_fakeItemAny, sut.Resolve(""));
        }

        [Test(Description = "returns an exact match")]
        public void GetExact()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);
            Assert.AreEqual(_fakeItem1, sut.Get("foo"));
        }

        [Test(Description = "returns null for prefix or non-matches")]
        public void GetNoMatch()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);

            Assert.IsNull(sut.Get("foo123"));
            Assert.IsNull(sut.Get("bar"));
            Assert.IsNull(sut.Get(""));
        }

        [Test(Description = "overwrites a value on double-insert")]
        public void Insert()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);
            sut.Insert("foo", _fakeItem2);

            Assert.AreEqual(new[] {"foo"}, sut.Prefixes);
            Assert.AreEqual(new Dictionary<string, Map> { {"foo", _fakeItem2}}, sut.Sources);
        }

        [Test(Description = "removes a prefix and the corresponding item")]
        public void Delete()
        {
            var sut = new PrefixMap<Map>();

            sut.Insert("foo", _fakeItem1);
            sut.Insert("bar", _fakeItem1);

            sut.Delete("bar");
            Assert.AreEqual(new[] {"foo"}, sut.Prefixes);
            Assert.AreEqual(new Dictionary<string, Map> { {"foo", _fakeItem1}}, sut.Sources);

            sut.Delete("foobar");
            Assert.AreEqual(new[] {"foo"}, sut.Prefixes);
            Assert.AreEqual(new Dictionary<string, Map> { {"foo", _fakeItem1}}, sut.Sources);

            sut.Delete("foo");
            Assert.AreEqual(new string[] {}, sut.Prefixes);
            Assert.AreEqual(new Map(), sut.Sources);
        }
    }
}
