using System;

namespace Interledger.Net.ILP.Interfaces.Routing
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
        DateTime? ExpiresAt { get; }
        object AdditionalInfo { get; }
        bool IsLocal { get; }
        string SourceAccount { get; }
        string DestinationAccount { get; }
        double[][] GetPoints { get; }
        double AmountAt(double x);
        double AmountReverse(double x);
        IRoute Combine(IRoute alternateRoute);
        IRoute Join(IRoute tailRoute, int expiryDuration);
        IRoute ShiftY(double dy);
        IRoute Simplify(int maxPoints);
        bool IsExpired(DateTime now);
        string ToJSON();
    }
}