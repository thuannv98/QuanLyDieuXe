using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace test_web.Models
{
    public class Geocoded_Waypoint
    {
        public string geocoder_status { get; set; }
        public string place_id { get; set; }
        public List<string> types { get; set; }
    }

    public class Point
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }
    public class Bound
    {
        public Point northeast { get; set; }
        public Point southwest { get; set; }
    }
    public class ObjectContaintsTextAndValueObj
    {
        public string text { get; set; }
        public double value { get; set; }
    }
    //public class Duration
    //{
    //    public string text { get; set; }
    //    public double value { get; set; }
    //}
    public class Polyline
    {
        public string points { get; set; }
    }
    public class Step
    {
        public ObjectContaintsTextAndValueObj distance { get; set; }
        public ObjectContaintsTextAndValueObj duration { get; set; }
        public Point end_location { get; set; }
        public string html_instructions { get; set; }
        public Polyline polyline { get; set; }
        public Point start_location { get; set; }
        public string travel_mode { get; set; }
    }
    public class Via_Waypoint
    {
        public Point location { get; set; }
        public int step_index { get; set; }
        public double step_interpolation { get; set; }
    }
    public class Leg
    {
        public ObjectContaintsTextAndValueObj distance { get; set; }
        public ObjectContaintsTextAndValueObj duration { get; set; }
        public string end_address { get; set; }
        public Point end_location { get; set; }
        public string start_address { get; set; }
        public Point start_location { get; set; }
        public List<Step> steps { get; set; }
        public List<double> traffic_speed_entry { get; set; }   // để sửa sau
        public List<Via_Waypoint> via_waypoint { get; set; }
    }
    public class Overview_Polyline
    {
        public string points { get; set; }
    }
    public class Route
    {
        public Bound bounds { get; set; }
        public string copyrights { get; set; }
        public List<Leg> legs { get; set; }
        public Overview_Polyline overview_polyline { get; set; }
        public string summary { get; set; }
        public List<string> warnings { get; set; }
        public List<int> waypoint_order { get; set; }
    }
    public class MapDirectionResponse
    {
        public List<Geocoded_Waypoint> geocoded_waypoints { get; set; }
        public List<Route> routes { get; set; }
        public string status { get; set; }
    }
}

