using System;

namespace EXOKeyCheck
{
    internal class OAReport
    {
        public int ReportID { get; set; }
        public string SerialNumber { get; set; }
        public string ProductKey { get; set; }
        public string ProductKeyID { get; set; }
        public string ProductKeyState { get; set; }
        public string ProductKeyPartNumber { get; set; }
        public string ActivationState { get; set; }
        public string HardwareHash { get; set; }
        public DateTime DateConsumed { get; set; }
        public DateTime? DateBound { get; set; }
        public string Source { get; set; }
    }
}
