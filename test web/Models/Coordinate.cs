using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace test_web.Models
{
    public class Coordinate
    {
        double latitude;
        double longitude;

        public double Latitude
        {
            get { return latitude; }
            set { latitude = value; }
        }
        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; }
        }

        public Coordinate()
        {

        }
        public Coordinate(Coordinate c)
        {
            this.latitude = c.Latitude;
            this.longitude = c.Longitude;
        }
        public Coordinate(double lat, double lng)
        {
            this.latitude = lat;
            this.longitude = lng;
        }

        public bool IsSame(Coordinate coor)
        {
            if (this.latitude == coor.latitude && this.longitude == coor.longitude)
                return true;
            else
                return false;
        }
    }
    public class Vector
    {
        double lat;
        double lng;
        public double Lat
        {
            get { return lat; }
            set { lat = value; }
        }
        public double Lng
        {
            get { return lng; }
            set { lng = value; }
        }
        public Vector()
        {

        }
        public Vector(double lat, double lng)
        {
            this.lat = lat;
            this.lng = lng;
        }
    }
}