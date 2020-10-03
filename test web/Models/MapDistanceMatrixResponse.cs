using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace test_web.Models
{
    public class Distance
    {
        public string text { get; set; }
        public double value { get; set; }
    }
    public class Duration
    {
        public string text { get; set; }
        public double value { get; set; }
    }
    public class Element
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public Duration duration_in_traffic { get; set; }
        public string status { get; set; }
    }
    public class Row
    {
        public List<Element> elements { get; set; }
    }
    public class MapDistanceMatrixResponse
    {
        public List<string> destination_addresses { get; set; }
        public List<string> origin_addresses { get; set; }
        public List<Row> rows { get; set; }
    }
}
