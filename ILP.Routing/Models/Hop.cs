namespace Interledger.Net.ILP.Routing.Models
{
    public class Hop
    {
        public IRoute BestRoute { get; set; }
        public bool IsFinal { get; set; }
        public bool IsLocal { get; set; }
        public string SourceLedger { get; set; }
        public double SourceAmount { get; set; }
        public string DestinationLedger { get; set; }
        public double DestinationAmount { get; set; }
        public string DestinationCreditAccount { get; set; }
        public string FinalLedger { get; set; }
        public double FinalAmount { get; set; }
        public int FinalPrecision { get; set; }
        public int FinalScale { get; set; }
        public int MinMessageWindow { get; set; }
        public object AdditionalInfo { get; set; }
        public double BestCost { get; set; }
        public string BestHop { get; set; }
        public double BestValue { get; set; }
    }
}
