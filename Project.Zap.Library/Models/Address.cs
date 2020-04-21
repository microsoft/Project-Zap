namespace Project.Zap.Library.Models
{
    public class Address
    {
        public string Text { get; set; }
        public string ZipOrPostcode { get; set; }
    }

    public class Coordinates
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }

}
