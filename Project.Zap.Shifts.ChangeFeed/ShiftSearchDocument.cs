using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Microsoft.Spatial;

namespace Project.Zap.Shifts.ChangeFeed
{
    public class ShiftSearchDocument
    {
        [Key]
        [IsRetrievable(true)]
        public string Id { get; set; }
        
        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public string LocationName { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public string City { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public string WorkType { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public DateTimeOffset StartDateTime { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true), IsFacetable]
        public DateTimeOffset EndDateTime { get; set; }

        [IsFilterable, IsSortable]
        public GeographyPoint Location { get; set; }
    }
}
