using System;

namespace Interledger.Net.ILP.Routing.Models
{
    public class RouteInfo
    {
        public int MinMessageWindow { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsLocal { get; set; }
        
        public string SourceAccount { get; set; }
        public string DestinationAccount { get; set; }
        public object AdditionalInfo { get; set; }
        public string TargetPrefix { get; set; }
    }
}
