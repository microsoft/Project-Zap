namespace Project.Zap.Library.Models
{
    public class Address
    {
        public string City { get; set; }
        public string ZipOrPostCode { get; set; }
    }

    public class Coordinates
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }

}
