using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using test_web.Models;

namespace test_web.Helpers
{
    public class DBSCAN
    {
        double eps;
        int minPts;
        List<Coordinate> data;
        string[] status;
        List<List<int>> clusters;
        List<int> noise;
        public string[] Status
        {
            get { return status; }
        }
        public List<List<int>> Clusters
        {
            get { return clusters; }
        }
        public List<int> Noise
        {
            get { return noise; }
        }
        public DBSCAN(List<Coordinate> RegData, double eps, int minPts)
        {
            status = new string[RegData.Count];
            clusters = new List<List<int>>();
            noise = new List<int>();
            data = RegData;
            this.eps = eps;
            this.minPts = minPts;


            for (int i = 0; i < RegData.Count; i++)
            {
                if (status[i] == null)
                {
                    status[i] = "0"; // visited and marked as noise by default
                    List<int> neighbours = get_region_neighbours(i);
                    int num_point = neighbours.Count + 1;
                    if (num_point < minPts)
                    {
                        noise.Add(i); // noise
                    }
                    else
                    {
                        clusters.Add(new List<int>()); // empty new cluster
                        int cluster_id = clusters.Count;
                        expand_cluster(i, neighbours, cluster_id);
                    }
                }
            }
        }

        // Core Algorithm Related
        List<int> get_region_neighbours(int point_id)
        {
            List<int> neighbours = new List<int>();
            Coordinate d = data[point_id];
            for (int i = 0; i < data.Count; i++)
            {
                if (point_id == i)
                {
                    continue;
                }
                if (haversine_distance(data[i], d) <= eps)
                {
                    neighbours.Add(i);
                }
            }

            return neighbours;
        }

        // Distance Functions
        public static double haversine_distance(Coordinate point1, Coordinate point2)
        {
            double R = 6371;
            double lat1 = (point1.Latitude * Math.PI) / 180;
            double lng1 = (point1.Longitude * Math.PI) / 180;
            double lat2 = (point2.Latitude * Math.PI) / 180;
            double lng2 = (point2.Longitude * Math.PI) / 180;

            double dLat = (lat2 - lat1);
            double dLng = (lng2 - lng1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c;
            //System.Console.WriteLine("kc: " + d);
            return d;
        }


        void expand_cluster(int point_id, List<int> neighbours, int cluster_id)
        {
            clusters[cluster_id - 1].Add(point_id); // thêm điểm vào cụm
            status[point_id] = cluster_id.ToString(); // đánh dấu điểm này thuộc cụm cluster_idx

            for (int i = 0; i < neighbours.Count; i++)
            {
                int curr_point_id = neighbours[i];
                if (status[curr_point_id] == null)//nếu điểm này chưa xét
                {
                    status[curr_point_id] = "0"; // đặt điểm này mặc định là nhiễu
                    List<int> neighbours_of_neighbour = get_region_neighbours(curr_point_id);
                    int num_point = neighbours_of_neighbour.Count + 1;

                    if (num_point >= minPts)
                    {
                        expand_cluster(curr_point_id, neighbours_of_neighbour, cluster_id);
                    }
                }

                if (Int32.Parse(status[curr_point_id]) < 1)
                {// neighbour này đã xét nhưng chưa thêm vào cụm
                    status[curr_point_id] = cluster_id.ToString();
                    clusters[cluster_id - 1].Add(curr_point_id); //thêm vào cụm
                }
            }
        }
    }
}
