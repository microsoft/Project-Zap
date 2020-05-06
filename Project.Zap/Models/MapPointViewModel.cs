using System;

namespace Project.Zap.Models
{
    public class MapPointViewModel
    {
        public string Location { get; set; }

        public DateTime Start { get; set; }

        public string Address { get; set; }

        public string ZipOrPostcode { get; set; }

        public int Quantity { get; set; }

        public int Available { get; set; }
        public PointViewModel Point { get; internal set; }
    }
}
