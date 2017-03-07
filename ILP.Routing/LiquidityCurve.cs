using System;
using System.Collections.Generic;
using System.Linq;
using Interledger.Net.ILP.Routing.Models;
using Simplifynet;

namespace Interledger.Net.ILP.Routing
{
    public class LiquidityCurve : ILiquidityCurve
    {
        public LiquidityCurve()
        {
            Points = new double[0][];
        }

        public LiquidityCurve(double[][] points)
        {
            Points = points;
        }

        public double[][] Points { get; }
        public double[][] GetPoints => Points;

        public double AmountAt(double x)
        {
            if (!Points.Any())
                return 0;

            if (x < Points[0][0])
                return 0;

            if (Equals(x, Points[0][0]))
                return Points[0][1];

            if (Points[Points.Length - 1][0] <= x)
                return Points[Points.Length - 1][1];

            var i = 0;
            while (Points[i][0] < x)
            {
                i++;
            }

            var pointA = Points[i - 1];
            var pointB = Points[i];

            return (pointB[1] - pointA[1])/(pointB[0] - pointA[0])*(x - pointA[0]) + pointA[1];
        }

        public double AmountReverse(double y)
        {
            if (Points[0][1] >= y)
                return Points[0][0];

            if (Points[Points.Length - 1][1] < y)
                return double.PositiveInfinity;

            var i = 0;
            while (Points[i][1] < y)
            {
                i++;
            }

            var pointA = Points[i - 1];
            var pointB = Points[i];

            return (pointB[0] - pointA[0])/(pointB[1] - pointA[1])*(y - pointA[1]) + pointA[0];
        }

        /// <summary>
        /// Simplify route to contain a maximum number of points.
        /// Uses the Visvalingam-Whyatt line simplification algorithm.
        /// https://github.com/imshz/simplify-net
        /// </summary>
        /// <param name="maxPoints"></param>
        /// <returns></returns>
        public ILiquidityCurve Simplify(int maxPoints)
        {
            var utility = new SimplifyUtility();
            var points = Points.Select(point => new Point(point[0], point[1])).ToArray();
            var simplified = utility.Simplify(points, maxPoints);

            var result = new double[simplified.Count][];
            for (var i = 0; i < simplified.Count; i++)
            {
                result[i][0] = simplified[i].X;
                result[i][1] = simplified[i].Y;
            }

            return new LiquidityCurve(result);
        }

        public ILiquidityCurve Combine(ILiquidityCurve curve)
        {
            var combinedPoints = MapToMax(curve.Points)
                .Concat(curve.MapToMax(Points))
                .Concat(CrossOvers(curve))                
                .OrderBy(p => p[0])
                .Distinct(new PointEqualityComparer())
                .ToArray();

            return new LiquidityCurve(combinedPoints);
        }

        /// <summary>
        /// A._mapToMax(B) to find [AB, A]
        /// B._mapToMax(A) to find[AB, B]
        /// 
        /// │              B
        /// │    A a a a a a
        /// │   a b
        /// │  a b
        /// │ AB
        /// └────────────────
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public double[][] MapToMax(double[][] points)
        {
            if (points.Length == 0)
                return points;

            return points
                .Select(point => new[] {point[0], Math.Max(point[1], AmountAt(point[0]))})
                .ToArray();
        }

        /// <summary>
        /// A._crossovers(B) to find [AB, ●]
        /// 
        /// │              B
        /// │    A a a a●a a
        /// │   a b
        /// │  a b
        /// │ AB
        /// └────────────────
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        private double[][] CrossOvers(ILiquidityCurve curve)
        {
            if (Points.Length == 0 || curve.Points.Length == 0)
                return new double[][] {};

            var endA = Points[Points.Length - 1];
            var endB = curve.Points[curve.Points.Length - 1];

            var pointsA = (double[][]) Points.Clone();
            var pointsB = (double[][]) curve.Points.Clone();

            if (endA[0] < endB[0])
            {
                var newPoint = new[] {new[] {endB[0], endA[1]}};
                pointsA = pointsA.Concat(newPoint).ToArray();

            }

            if (endB[0] < endA[0])
            {
                var newPoint = new[] {new[] {endA[0], endB[1]}};
                pointsB = pointsB.Concat(newPoint).ToArray();
            }

            double[][] result;
            EachOverlappingSegment(pointsA, pointsB, out result);

            return result;
        }

        private static void EachOverlappingSegment(double[][] pointsA, double[][] pointsB, out double[][] result)
        {
            result = new double[][] {};

            var cursor = 1;
            for (var indexA = 1; indexA < pointsA.Length; indexA++)
            {
                var lineA = ToLine(pointsA[indexA - 1], pointsA[indexA]);
                for (var indexB = cursor; indexB < pointsB.Length; indexB++)
                {
                    var lineB = ToLine(pointsB[indexB - 1], pointsB[indexB]);
                    if (lineB.X1 < lineA.X0)
                    {
                        cursor++;
                        continue;
                    }

                    if (lineA.X1 < lineB.X0)
                        break;

                    var solution = IntersectLineSegments(lineA, lineB);
                    if (solution != null)
                        result = result.Concat(solution).ToArray();

                }
            }
        }

        public ILiquidityCurve Join(ILiquidityCurve curve)
        {
            var points = (Points
                .Select(p => new[] { p[0], curve.AmountAt(p[1]) })
                .Concat(curve.Points
                    .Select(p => new[] { AmountReverse(p[0]), p[1] })))
                    .Where(p => !double.IsPositiveInfinity(p[0]))
                    .OrderBy(p => p[0])
                    .Distinct(new PointEqualityComparer())
                    .ToArray();
            
            return new LiquidityCurve(points);

        }
        
        public ILiquidityCurve ShiftY(double dy)
        {
            var shifted = (double[][]) Points.Clone();
            for (var i = 0; i < Points.Length; i++)
            {
                shifted[i][1] = shifted[i][1] + dy;
            }

            return new LiquidityCurve(shifted);
        }

        /// <summary>
        ///
        ///      y₁ - y₀       x₁y₀ - x₀y₁
        /// y = ───────── x + ───────────── = mx + b
        /// x₁ - x₀         x₁ - x₀
        /// </summary>
        /// <param name="pA"></param>
        /// <param name="pB"></param>
        /// <returns></returns>
        private static Line ToLine(double[] pA, double[] pB)
        {
            var x0 = pA[0];
            var x1 = pB[0];
            var y0 = pA[1];
            var y1 = pB[1];
            var dx = x1 - x0;
            var m = dx.Equals(0) ? 0 : (y1 - y0) / dx;
            var b = dx.Equals(0) ? 0 : (x1 * y0 - x0 * y1) / dx;

            return new Line { M = m, B = b, X0 = x0, X1 = x1 };
        }

        /// <summary>
        /// y = m₀x + b₀ = m₁x + b₁
        ///
        ///      b₁ - b₀
        /// x = ───────── ; line0.x₀ ≤ x ≤ line0.x₁ and line1.x₀ ≤ x ≤ line1.x₁
        ///      m₀ - m₁
        /// </summary>
        /// <param name="line0"></param>
        /// <param name="line1"></param>
        private static double[][] IntersectLineSegments(Line line0, Line line1)
        {

            if (line0.M.Equals(line1.M))
                return null;

            var x = (line1.B - line0.B) / (line0.M - line1.M);
            var y = line0.M * x + line0.B;

            // Verify that the solution is in the domain.
            if (x < line0.X0 || line0.X1 < x)
                return null;

            if (x < line1.X0 || line1.X1 < x)
                return null;

            return new[] {new[] {x, y}};
        }

        private class PointEqualityComparer : IEqualityComparer<double[]>
        {
            public bool Equals(double[] x, double[] y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(double[] obj)
            {
                var hash = 19;

                foreach (var o in obj)
                {
                    hash = hash * 31 + o.GetHashCode();
                }

                return hash;
            }
        }
    }
}