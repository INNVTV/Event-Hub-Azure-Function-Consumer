using System;
using System.Collections.Generic;
using System.Text;

namespace EventProcessor.Models
{
    public class TelemetryData
    {
        public string DedviceId { get; set; }
        public int Temperature { get; set; }
        public int Order { get; set; }
    }
}
