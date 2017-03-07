namespace Interledger.Net.ILP.Interfaces.Routing
{
    public interface ILiquidityCurve
    {
        double[][] Points { get; }
        double[][] GetPoints { get; }
        double AmountAt(double x);
        double AmountReverse(double y);
        double[][] MapToMax(double[][] points);
        ILiquidityCurve Simplify(int maxPoints);
        ILiquidityCurve Combine(ILiquidityCurve curve);
        ILiquidityCurve Join(ILiquidityCurve curve);
        ILiquidityCurve ShiftY(double dy);
    }
}