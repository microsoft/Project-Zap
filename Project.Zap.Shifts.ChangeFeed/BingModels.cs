using System.Collections.Generic;

namespace Project.Zap.Shifts.ChangeFeed
{
    public class BingMapResponse
    {
        public string copyright { get; set; }

        public List<BingMapResourceSet> resourceSets { get; set; }
    }

    public class BingMapResourceSet
    {
        public List<BingMapResource> resources { get; set; }
    }

    public class BingMapResource
    {
        public BingMapPoint point { get; set; }
    }

    public class BingMapPoint
    {
        public string type { get; set; }

        public double[] coordinates { get; set; }
    }
}
