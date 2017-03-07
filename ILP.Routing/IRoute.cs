using System;

namespace Interledger.Net.ILP.Routing
{
    public interface IRoute
    {
        ILiquidityCurve Curve { get; }
        string[] Hops { get; }
        string SourceLedger { get; }
        string NextLedger { get; }
        string DestinationLedger { get; }
        string TargetPrefix { get; }
        int MinMessageWindow { get; }
        DateTime? ExpiresAt { get; set; }
        object AdditionalInfo { get; }
        bool IsLocal { get; set; }
        string SourceAccount { get; }
        string DestinationAccount { get; }
        double[][] GetPoints { get; }
        int DestinationPrecision { get; set; }
        int DestinationScale { get; set; }
        double AmountAt(double x);
        double AmountReverse(double x);
        IRoute Combine(IRoute alternateRoute);
        IRoute Join(IRoute tailRoute, TimeSpan expiryDuration);
        IRoute ShiftY(double dy);
        IRoute Simplify(int maxPoints);
        bool IsExpired(DateTime now);
        string ToJSON();
        void BumpExpiration(TimeSpan holdDownTime);
    }
}