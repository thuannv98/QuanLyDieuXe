using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using test_web.Models;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;

namespace test_web.Helpers
{
    public static class MapFunction
    {
        public static string Mapkey { private get; set; }
        static int countRequest;
        public static int CountRequest { get { return countRequest; } }

        public static string GetGeoCodeUrl(double lat, double lng)
        {
            return "https://maps.googleapis.com/maps/api/geocode/json?latlng=" + lat + "," + lng + "&key=" + Mapkey;
        }

        public static async Task<string> GetJsonAsync(string url)
        {
            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            string result = await client.GetStringAsync(url);
            countRequest++;
            return result;
        }

        public static async Task<MapDirectionResponse> GetDirectionJsonAsync(Coordinate origin, Coordinate destination, List<Coordinate> waypoints = null)
        {
            string str_origin = "origin=" + origin.Latitude + "," + origin.Longitude;
            string str_destination = "destination=" + destination.Latitude + "," + destination.Longitude;

            string mode = "mode=driving";

            string parameter = str_origin + "&" + str_destination + "&" + mode;
            if (waypoints != null && waypoints.Count > 0)
            {
                parameter += "&waypoints=";
                int count = waypoints.Count;
                for (int i = 0; i < count; i++)
                {
                    if (i == count - 1)
                        parameter += waypoints[i].Latitude + "," + waypoints[i].Longitude;
                    else
                        parameter += waypoints[i].Latitude + "," + waypoints[i].Longitude + "|";
                }
            }

            string output = "json";

            string key = "key=" + Mapkey;

            string url = "https://maps.googleapis.com/maps/api/directions/" + output + "?" + parameter + "&" + key;
            System.Console.WriteLine("url: " + url);
            string json = await GetJsonAsync(url);
            //System.Console.WriteLine("js: " + json);


            return JsonConvert.DeserializeObject<MapDirectionResponse>(json);
        }

        public static async Task<double> GetDistanceAsync(Coordinate origin, Coordinate destination, List<Coordinate> waypoints = null)
        {
            MapDirectionResponse mapDirectionResponse = await GetDirectionJsonAsync(origin, destination, waypoints);
            double distance = 0;
            foreach (Leg leg in mapDirectionResponse.routes[0].legs)
            {
                distance += leg.distance.value;
            }
            return distance;
        }

        public static IEnumerable<Coordinate> GetOverviewPolylinePoints(MapDirectionResponse mapDirectionResponse)
        {
            string overview_polyline = mapDirectionResponse.routes[0].overview_polyline.points;
            return Decode(overview_polyline);
        }

        public static IEnumerable<Coordinate> Decode(string polylineString)
        {
            if (string.IsNullOrEmpty(polylineString))
                throw new ArgumentNullException(nameof(polylineString));

            var polylineChars = polylineString.ToCharArray();
            var index = 0;

            var currentLat = 0;
            var currentLng = 0;

            while (index < polylineChars.Length)
            {
                // Next lat
                var sum = 0;
                var shifter = 0;
                int nextFiveBits;
                do
                {
                    nextFiveBits = polylineChars[index++] - 63;
                    sum |= (nextFiveBits & 31) << shifter;
                    shifter += 5;
                } while (nextFiveBits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                // Next lng
                sum = 0;
                shifter = 0;
                do
                {
                    nextFiveBits = polylineChars[index++] - 63;
                    sum |= (nextFiveBits & 31) << shifter;
                    shifter += 5;
                } while (nextFiveBits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length && nextFiveBits >= 32)
                    break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                yield return new Coordinate(Convert.ToDouble(currentLat) / 1E5, Convert.ToDouble(currentLng) / 1E5);
                //{
                //    Latitude = 
                //    Longitude = 
                //};
            }
        }
        /// <summary>
        /// Mã hóa một danh sách các tọa độ thành chuỗi được mã hóa
        /// </summary>
        /// <param name="points">Danh sách tọa độ</param>
        /// <returns></returns>
        public static string Encode(IEnumerable<Coordinate> points)
        {
            var str = new StringBuilder();

            var encodeDiff = (Action<int>)(diff => {
                int shifted = diff << 1;
                if (diff < 0)
                    shifted = ~shifted;
                int rem = shifted;
                while (rem >= 0x20)
                {
                    str.Append((char)((0x20 | (rem & 0x1f)) + 63));
                    rem >>= 5;
                }
                str.Append((char)(rem + 63));
            });

            int lastLat = 0;
            int lastLng = 0;
            foreach (var point in points)
            {
                int lat = (int)Math.Round(point.Latitude * 1E5);
                int lng = (int)Math.Round(point.Longitude * 1E5);
                encodeDiff(lat - lastLat);
                encodeDiff(lng - lastLng);
                lastLat = lat;
                lastLng = lng;
            }
            return str.ToString();
        }

        /// <summary>
        /// Lấy ma trận khoảng cách từ Google Map Distance Matrix API
        /// </summary>
        /// <param name="origins">Danh sách điểm xuất phát</param>
        /// <param name="destinations">Danh sách điểm kết thúc</param>
        /// <param name="departure_time">Thời gian khởi hành: Tổng thời gian tính bằng giây kể từ nữa đêm 1/1/1970 UTC (tùy chọn)</param>
        /// <returns></returns>
        public static async Task<MapDistanceMatrixResponse> GetDistanceMatrixResponse(List<Coordinate> origins, List<Coordinate> destinations, int departure_time = 0)
        {
            string str_origin = "";
            string str_destination = "";
            int OrgCount = origins.Count;
            int DesCount = destinations.Count;
            string[] OriginsSets = new string[OrgCount / 10 + (OrgCount % 10 > 0 ? 1 : 0)];
            string[] DestinationsSets = new string[DesCount / 10 + (DesCount % 10 > 0 ? 1 : 0)];
            for (int i = 0; i < OrgCount; i++)
            {
                if (i % 10 == 0)
                {
                    str_origin = "origins=";
                }
                if (i == OrgCount - 1 || i % 10 == 9)
                    str_origin += origins[i].Latitude + "," + origins[i].Longitude;
                else
                    str_origin += origins[i].Latitude + "," + origins[i].Longitude + "|";
                if ((i + 1) % 10 == 0 || i == OrgCount - 1)
                    OriginsSets[i / 10] = str_origin;
            }

            for (int i = 0; i < DesCount; i++)
            {
                if (i % 10 == 0)
                {
                    str_destination = "destinations=";
                }
                if (i == DesCount - 1 || i % 10 == 9)
                    str_destination += destinations[i].Latitude + "," + destinations[i].Longitude;
                else
                    str_destination += destinations[i].Latitude + "," + destinations[i].Longitude + "|";
                if ((i + 1) % 10 == 0 || i == DesCount - 1)
                    DestinationsSets[i / 10] = str_destination;
            }
            string mode = "mode=driving";

            //string parameter = str_origin + "&" + str_destination + "&" + mode;

            string output = "json";

            string key = "key=" + Mapkey;

            string departure_time_param = departure_time == 0 ? "" : "&departure_time=" + departure_time;

            MapDistanceMatrixResponse mapDistanceMatrixResponse = new MapDistanceMatrixResponse
            {
                destination_addresses = new List<string>(),
                origin_addresses = new List<string>(),
                rows = new List<Row>()
            };
            bool have_destination_addresses = false;
            for (int i = 0; i < OriginsSets.Length; i++)
            {
                string OriginsSet = OriginsSets[i];
                MapDistanceMatrixResponse mapDistanceMatrixRes = new MapDistanceMatrixResponse
                {
                    destination_addresses = new List<string>(),
                    origin_addresses = new List<string>(),
                    rows = new List<Row>()
                };
                bool have_origin_addresses = false;
                for (int j = 0; j < DestinationsSets.Length; j++)
                {
                    string DestinationsSet = DestinationsSets[j];
                    string parameter = OriginsSet + "&" + DestinationsSet + "&" + mode;

                    string url = "https://maps.googleapis.com/maps/api/distancematrix/" + output + "?" + parameter + departure_time_param + "&" + key;

                    //System.Console.WriteLine("Distance Matrix Request: " + url);

                    string json = await GetJsonAsync(url);

                    MapDistanceMatrixResponse mapDistanceMatrixRes1 = JsonConvert.DeserializeObject<MapDistanceMatrixResponse>(json);

                    if (!have_origin_addresses)
                    {
                        mapDistanceMatrixResponse.origin_addresses.AddRange(mapDistanceMatrixRes1.origin_addresses);
                        have_origin_addresses = true;
                    }

                    if (!have_destination_addresses)
                        mapDistanceMatrixResponse.destination_addresses.AddRange(mapDistanceMatrixRes1.destination_addresses);

                    if (j == 0)
                    {
                        mapDistanceMatrixRes.rows.AddRange(mapDistanceMatrixRes1.rows);
                        continue;
                    }
                    for (int k = 0; k < mapDistanceMatrixRes1.rows.Count; k++)
                    {
                        mapDistanceMatrixRes.rows[k].elements.AddRange(mapDistanceMatrixRes1.rows[k].elements);
                    }
                }
                have_destination_addresses = true;
                mapDistanceMatrixResponse.rows.AddRange(mapDistanceMatrixRes.rows);
            }
            return mapDistanceMatrixResponse;
        }

        public static List<List<double>> GetDistanceMatrix(MapDistanceMatrixResponse mapDistanceMatrixResponse)
        {
            try
            {
                int rowcount = mapDistanceMatrixResponse.rows.Count;
                List<List<double>> distancematrix = new List<List<double>>();
                for (int i = 0; i < rowcount; i++)
                {
                    Row row = mapDistanceMatrixResponse.rows[i];
                    distancematrix.Add(new List<double>());
                    foreach (Element element in row.elements)
                    {
                        distancematrix[i].Add(element.distance.value);
                    }
                }
                return distancematrix;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<List<double>> GetDurationMatrix(MapDistanceMatrixResponse mapDistanceMatrixResponse)
        {
            try
            {
                int rowcount = mapDistanceMatrixResponse.rows.Count;
                List<List<double>> durationmatrix = new List<List<double>>();
                for (int i = 0; i < rowcount; i++)
                {
                    Row row = mapDistanceMatrixResponse.rows[i];
                    durationmatrix.Add(new List<double>());
                    foreach (Element element in row.elements)
                    {
                        durationmatrix[i].Add(element.duration.value);
                    }
                }
                return durationmatrix;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static List<List<double>> Get_Duration_in_traffic_Matrix(MapDistanceMatrixResponse mapDistanceMatrixResponse)
        {
            try
            {
                int rowcount = mapDistanceMatrixResponse.rows.Count;
                List<List<double>> duration_in_traffic_matrix = new List<List<double>>();
                for (int i = 0; i < rowcount; i++)
                {
                    Row row = mapDistanceMatrixResponse.rows[i];
                    duration_in_traffic_matrix.Add(new List<double>());
                    foreach (Element element in row.elements)
                    {
                        duration_in_traffic_matrix[i].Add(element.duration_in_traffic.value);
                    }
                }
                return duration_in_traffic_matrix;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static double timgoc(Vector vectorU, Vector vectorV)
        {
            //u, v là vector(x, y)
            double tichvohuong = vectorU.Lat * vectorV.Lat + vectorU.Lng * vectorV.Lng;
            double DoDaiVectorU = Math.Sqrt(Math.Pow(vectorU.Lat, 2) + Math.Pow(vectorU.Lng, 2));
            double DoDaiVectorV = Math.Sqrt(Math.Pow(vectorV.Lat, 2) + Math.Pow(vectorV.Lng, 2));
            double cosx = tichvohuong / (DoDaiVectorU * DoDaiVectorV);

            return Math.Acos(cosx) * 180 / Math.PI;
        }

        public static Vector layVector(Coordinate p1, Coordinate p2)
        {
            return new Vector(p2.Latitude - p1.Latitude, p2.Longitude - p1.Longitude);
        }
        public static double tongquangduong = 0;
        public static async Task<string> GetPolyline(Coordinate origin, Coordinate destination, List<Coordinate> waypoints = null)
        {
            MapDirectionResponse MDR = await MapFunction.GetDirectionJsonAsync(origin, destination, waypoints);

            if (MDR.routes.Count == 0)
                return null;
            tongquangduong += MDR.routes.FirstOrDefault().legs.FirstOrDefault().distance.value;

            List<Coordinate> PolylinePoints = new List<Coordinate>();

            foreach (Leg leg in MDR.routes[0].legs)
            {
                foreach (Step step in leg.steps)
                {
                    List<Coordinate> miniPolylinePoints = MapFunction.Decode(step.polyline.points).ToList();
                    miniPolylinePoints.Remove(miniPolylinePoints.Last());

                    PolylinePoints.AddRange(miniPolylinePoints);
                }
            }
            Coordinate lastPolylinePoint = MapFunction.Decode(MDR.routes[0].legs.Last().steps.Last().polyline.points).Last();
            PolylinePoints.Add(lastPolylinePoint);

            string Polyline = MapFunction.Encode(PolylinePoints);

            return Polyline;
        }
    }
}
