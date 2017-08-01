using System;

namespace ThatPiSample.Models
{
    public class BeaconSighting
    {
        public ulong Address { get; set; }
        public string Namespace { get; set; }
        public string Instance { get; set; }
        public Guid Uuid { get; set; }
        public short? BaseTransmitPower { get; set; }
        public int? Temperature { get; set; }
        public int? BatteryVoltage { get; set; }
        public Uri PublishedUrl { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public double SignalStrength { get; set; }
    }
}
