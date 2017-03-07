using Interledger.Net.ILP.Routing;
using NUnit.Framework;

namespace ILP.Routing.Tests
{
    [TestFixture(Description = "LiquidityCurve")]
    public class LiquidityCurveTests
    {
        [Test(Description = "it saves the points")]
        public void Constructor()
        {
            var points = new double[0][];
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(points, curve.GetPoints);
        }

        [Test(Description = "returns 0 if 'x' is too low")]
        public void AmountAtReturnZero()
        {
            var points = new[] {new[] {10D, 20D}, new[] {100D, 200D}};
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(0, curve.AmountAt(0));
            Assert.AreEqual(0, curve.AmountAt(-10));
        }

        [Test(Description = "returns the maximum if 'x' is too high")]
        public void AmountAtReturnMax()
        {
            var points = new[] { new[] { 10D, 20D }, new[] { 100D, 200D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(200, curve.AmountAt(101));
            Assert.AreEqual(200, curve.AmountAt(1000));
        }

        [Test(Description = "returns the linear interpolation of intermediate 'x' values")]
        public void AmountAtLinearInterpolation()
        {
            var points = new[] { new[] { 10D, 20D }, new[] { 100D, 200D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(20, curve.AmountAt(10));
            Assert.AreEqual(22, curve.AmountAt(11));
            Assert.AreEqual(110, curve.AmountAt(55));
            Assert.AreEqual(200, curve.AmountAt(100));
        }

        [Test(Description = "returns an exact 'y' value when possible")]
        public void AmountAtExact()
        {
            var points = new[] { new[] { 0D, 0D }, new[] { 50D, 100D }, new[] { 100D, 1000D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(curve.AmountAt(50), 100);
        }

        [Test(Description = "returns the minimum 'x' if 'y' is too low")]
        public void AmountReverseMinimum()
        {
            var points = new[] { new[] { 10D, 20D }, new[] { 100D, 200D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(10, curve.AmountReverse(0));
            Assert.AreEqual(10, curve.AmountReverse(-10));
        }
    
        [Test(Description = "returns Infinity if 'y' is too high")]
        public void AmountReverseInfinity()
        {
            var points = new[] { new[] { 10D, 20D }, new[] { 100D, 200D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(double.PositiveInfinity,  curve.AmountReverse(201));
            Assert.AreEqual(double.PositiveInfinity, curve.AmountReverse(1000));
        }

        [Test(Description = "returns the linear interpolation of intermediate 'y' values")]
        public void AmountReverseLinearInterpolation()
        {
            var points = new[] { new[] { 10D, 20D }, new[] { 100D, 200D } };
            var curve = new LiquidityCurve(points);

            Assert.AreEqual(10, curve.AmountReverse(20), 10);
            Assert.AreEqual(11, curve.AmountReverse(22), 11);
            Assert.AreEqual(55, curve.AmountReverse(110), 55);
            Assert.AreEqual(100, curve.AmountReverse(200), 100);
        }

        [Test(Description = "finds an intersection between a slope and a flat line")]
        public void CombineIntersectionBetweenSlopeAndFlatLine()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 50D, 60D } };
            var curve1 = new LiquidityCurve(points1);
            var points2 = new[] { new[] { 0D, 0D }, new[] { 100D, 100D } };
            var curve2 = new LiquidityCurve(points2);

            var combinedCurve = curve1.Combine(curve2);

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 50D, 60D }, new[] { 60D, 60D }, new[] { 100D, 100D } };
            Assert.AreEqual(expectedPoints, combinedCurve.GetPoints);
            Assert.AreEqual(30, combinedCurve.AmountAt(25));
            Assert.AreEqual(60, combinedCurve.AmountAt(50));
            Assert.AreEqual(60, combinedCurve.AmountAt(60));
            Assert.AreEqual(70, combinedCurve.AmountAt(70));
        }

        [Test(Description = "ignores an empty curve")]
        public void CombineIgnoreEmpty()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 50D, 60D } };
            var curve1 = new LiquidityCurve(points1);
            var curve2 = new LiquidityCurve(new  double[][] {});

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 50D, 60D } };
            Assert.AreEqual(expectedPoints, curve1.Combine(curve2).GetPoints);
            Assert.AreEqual(expectedPoints, curve2.Combine(curve1).GetPoints);
        }

        [Test(Description = "ignores duplicate points")]
        public void CombineIgnoreDuplicates()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 50D, 60D }, new[] { 50D, 60D } };
            var curve1 = new LiquidityCurve(points1);
            var points2 = new[] { new[] { 0D, 0D }, new[] { 0D, 0D }, new[] { 100D, 100D } };
            var curve2 = new LiquidityCurve(points2);

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 50D, 60D }, new[] { 60D, 60D }, new[] { 100D, 100D } };
            Assert.AreEqual(expectedPoints, curve1.Combine(curve2).GetPoints);
        }

        [Test(Description = "finds an intersection between two slopes")]
        public void CombineIntersectionBetweenTwoSlopes()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 100D, 1000D } };
            var curve1 = new LiquidityCurve(points1);
            var points2 = new[] { new[] { 0D, 0D }, new[] { 100D/3D, 450D }, new[] { 200D/3D, 550D } };
            var curve2 = new LiquidityCurve(points2);

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 100D / 3D, 450D }, new[] { 50D, 500D }, new[] { 200D / 3D, 666.6666666666667D }, new[] { 100D, 1000D } };
            Assert.AreEqual(expectedPoints, curve1.Combine(curve2).GetPoints);
        }

        [Test(Description = "composes two routes")]
        public void JoinComposeTwoRoutes()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 200D, 100D } };
            var curve1 = new LiquidityCurve(points1);
            var points2 = new[] { new[] { 0D, 0D }, new[] { 50D, 60D } };
            var curve2 = new LiquidityCurve(points2);

            var joinedCurve = curve1.Join(curve2);

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 100D, 60D }, new[] { 200D, 60D } };
            Assert.AreEqual(expectedPoints, joinedCurve.GetPoints);
            Assert.AreEqual(30, joinedCurve.AmountAt(50));
            Assert.AreEqual(60, joinedCurve.AmountAt(100));
            Assert.AreEqual(60, joinedCurve.AmountAt(200));
        }

        [Test(Description = "truncates the domain as necessary")]
        public void JoinTruncateDomain()
        {
            var points1 = new[] { new[] { 0D, 0D }, new[] { 50D, 100D } };
            var curve1 = new LiquidityCurve(points1);
            var points2 = new[] { new[] { 0D, 0D }, new[] { 200D, 300D } };
            var curve2 = new LiquidityCurve(points2);

            var joinedCurve = curve1.Join(curve2);

            var expectedPoints = new[] { new[] { 0D, 0D }, new[] { 50D, 150D } };
            Assert.AreEqual(expectedPoints, joinedCurve.GetPoints);
        }

        [Test(Description = "shifts all of the points' Ys by the specified amount")]
        public void ShiftY()
        {
            var points = new[] { new[] { 0D, 0D }, new[] { 50D, 60D }, new[] { 100D, 100D } };
            var curve = new LiquidityCurve(points);

            var shiftedPoints = curve.ShiftY(1);

            var expectedpoints = new[] { new[] { 0D, 1D }, new[] { 50D, 61D }, new[] { 100D, 101D } };
            Assert.AreEqual(expectedpoints, shiftedPoints.GetPoints);
        }
    }
}
