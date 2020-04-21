namespace Project.Zap.Library.Models
{
    public class Address
    {
        public string Text { get; set; }
        public string ZipOrPostcode { get; set; }

        public Point Point { get; set; }
    }

    public class Point
    {
        public string type { get; set; } = "Point";

        public double[] coordinates { get; set; }
    }

}
