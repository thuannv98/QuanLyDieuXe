using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using test_web.Models;

namespace test_web.Helpers
{
    public class Point
    {
        public Coordinate location { get; set; }
        public string diachi { get; set; }
        public double distance { get; set; }
        public TimeSpan thoigianxeden_luotdi { get; set; }
        public TimeSpan thoigianxeden_luotve { get; set; }

        public List<point_detail> dsdiemcungtoado { get; set; }
    }
    public class point_detail
    {
        public int id { get; set; }
        public int songuoi { get; set; }
        public TimeSpan thoigianden { get; set; }
        public TimeSpan thoigianve { get; set; }
        public TimeSpan[] khungthoigianden { get; set; }
        public TimeSpan[] khungthoigianve { get; set; }
        public int DangKyLichChiTietId { get; set; }
        public int DangKyLichId { get; set; }
        public string NoiDi { get; set; }
        public string NoiDen { get; set; }
    }

    public class ChuyenDi
    {
        public List<Point> dsDiem { get; set; }
        public string DiaChiCuoiCung { get; set; }
        public int tongsonguoi { get; set; }
        public Xe xedi { get; set; }
        public List<Chitietnguoidi> listChitietnguoidi { get; set; }
    }
    class Xe_v2
    {
        public Xe ThongTinXe { get; set; }
        public bool trangthai { get; set; }
        public int songuoihientai { get; set; }
    }

    public class GomNhom
    {
        List<tblDangKyLichChiTiet> lichdangky;
        List<Xe> xes;
        dieuxe1Entities db;

        public List<ChuyenDi> DsChuyenDi;
        public List<LichDieuXe> DsDieuXe;

        const double eps = 5;   // Kilomet
        const int minPts = 2;

        public string json1 = "", json2 = "";
        public GomNhom()
        {

        }
        public GomNhom(List<tblDangKyLichChiTiet> data)
        {
            if (data == null)
                return;
            this.lichdangky = data;

            MapFunction.Mapkey = "AIzaSyDdwwJvAyOQWpSPj8pUxnDH8Whe9X-BxhA";

            DsChuyenDi = new List<ChuyenDi>();
            DsDieuXe = new List<LichDieuXe>();

            db = new dieuxe1Entities();
            xes = (from xe in db.tblXes
                   join loaixe in db.tblLoaiXes on xe.MaLoai equals loaixe.Id
                   where xe.TrangThai == 0
                   select new Xe() 
                   {
                       BienSoXe = xe.BienSoXe,
                       SoCho = loaixe.SoChoNgoi
                   }).ToList();

            //MiddleCall();
        }
        void MiddleCall()
        {
            ThucHien();
        }
        public async Task ThucHien()
        {
            var watchh = System.Diagnostics.Stopwatch.StartNew();

            int[] status = new int[lichdangky.Count];   //mặc định tất cả các lịch đăng ký chưa được xét

            List<Coordinate> Original_point = new List<Coordinate>();
            List<Coordinate> Destination_point = new List<Coordinate>();
            foreach (var dangky in lichdangky)
            {
                Coordinate point1 = new Coordinate();
                Coordinate point2 = new Coordinate();
                double[] toadonoidi = CoordinatesToArray(dangky.ToaDoDi);
                point1.Latitude = toadonoidi[0];
                point1.Longitude = toadonoidi[1];
                Original_point.Add(point1);

                double[] toadonoiden = CoordinatesToArray(dangky.ToaDoDen);
                point2.Latitude = toadonoiden[0];
                point2.Longitude = toadonoiden[1];
                Destination_point.Add(point2);

            }

            DateTime ngaydi = (DateTime)lichdangky.FirstOrDefault().NgayDen;
            var departure_time = ngaydi - new DateTime(1970, 1, 1);    // 0h (0h + múi giờ 7 ở việt nam = 7h sáng)
            var departure_time_seconds = (Int32)departure_time.TotalSeconds;
            //lấy ma trận khoảng cách
            List<Coordinate> danhsachdiem = new List<Coordinate>(Original_point);
            danhsachdiem.AddRange(Destination_point);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            MapDistanceMatrixResponse mapDistanceMatrixResponse = await MapFunction.GetDistanceMatrixResponse(danhsachdiem, danhsachdiem, departure_time_seconds);
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine("thời gian lấy ma trận khoảng cách: {0}", watch.Elapsed.TotalSeconds);

            List<List<double>> distancematrix = MapFunction.GetDistanceMatrix(mapDistanceMatrixResponse);
            List<List<double>> durationmatrix = MapFunction.GetDurationMatrix(mapDistanceMatrixResponse);
            List<List<double>> duration_in_traffic_matrix = MapFunction.Get_Duration_in_traffic_Matrix(mapDistanceMatrixResponse);
            List<Xe_v2> dsXe = new List<Xe_v2>();
            foreach (var xe in xes)
            {
                dsXe.Add(new Xe_v2 { ThongTinXe = xe, trangthai = false });
            }
            dsXe.Sort((x, y) => x.ThongTinXe.SoCho.CompareTo(y.ThongTinXe.SoCho));

            if (lichdangky.Count == 1)
                themchuyendivaodanhsach(DsChuyenDi, 1);
            else
            {
                DBSCAN Original = new DBSCAN(Original_point, eps, minPts);
                DBSCAN Destination = new DBSCAN(Destination_point, eps, minPts);

                List<List<int>> OrgCluster = Original.Clusters;
                List<int> OrgNoise = Original.Noise;

                List<List<int>> DesCluster = Destination.Clusters;
                List<int> DesNoise = Destination.Noise;

                //phân theo tọa độ
                List<List<Point>> dsChuyenDiTheoDiaDiem = new List<List<Point>>();

                for (int k = 0; k < OrgCluster.Count; k++)
                {
                    List<int> Ocluster = OrgCluster[k];
                    for (int l = 0; l < DesCluster.Count; l++)
                    {
                        List<int> Dcluster = DesCluster[l];
                        int countMatch = 0;
                        List<int> id = new List<int>();

                        foreach (int Opoint in Ocluster)
                        {
                            foreach (int Dpoint in Dcluster)
                            {
                                if (Opoint == Dpoint)
                                {
                                    id.Add(Opoint);
                                    countMatch++;
                                }
                            }
                        }

                        if (countMatch >= 2)
                        {
                            List<Point> chuyendi = new List<Point>();
                            Coordinate org = new Coordinate(Original_point[id[0]].Latitude, Original_point[id[0]].Longitude);

                            foreach (int IdPoint in id)    //orgpoint
                            {
                                status[IdPoint] = 1;   //đánh dấu lịch đăng ký này là đã xét

                                Coordinate newlocation = new Coordinate(Original_point[IdPoint].Latitude, Original_point[IdPoint].Longitude);
                                bool isExist = false;
                                foreach (var point in chuyendi)
                                {
                                    if (newlocation.IsSame(point.location))
                                    {
                                        isExist = true;
                                        point.dsdiemcungtoado.Add(new point_detail { id = IdPoint });
                                        break;
                                    }
                                }
                                if (isExist)
                                    continue;
                                chuyendi.Add(new Point
                                {
                                    location = newlocation,
                                    distance = 0,
                                    dsdiemcungtoado = new List<point_detail>()
                                    {
                                        new point_detail
                                        {
                                            id = IdPoint
                                        }
                                    }
                                });
                            }

                            foreach (int IdPoint in id)    //despoint
                            {
                                Coordinate newlocation = new Coordinate(Destination_point[IdPoint].Latitude, Destination_point[IdPoint].Longitude);

                                // thiết lập khung thời gian cho các điểm Destination  
                                //string[] khungthoigianden = laykhungthoigianden(lichdangky[IdPoint].GioDen, durationmatrix[IdPoint][getDestinationID(IdPoint)]);
                                //string[] khungthoigianve = laykhungthoigianve(lichdangky[IdPoint].GioVe, durationmatrix[IdPoint][getDestinationID(IdPoint)]);

                                bool isExist = false;
                                foreach (var point in chuyendi)
                                {
                                    if (newlocation.IsSame(point.location))
                                    {
                                        isExist = true;
                                        point.dsdiemcungtoado.Add(new point_detail
                                        {
                                            id = getDestinationID(IdPoint),
                                            //khungthoigianden = khungthoigianden,
                                            //khungthoigianve = khungthoigianve
                                        });
                                        break;
                                    }
                                }
                                if (isExist)
                                    continue;
                                chuyendi.Add(new Point
                                {
                                    location = newlocation,
                                    distance = 0,
                                    dsdiemcungtoado = new List<point_detail>()
                                    {
                                        new point_detail
                                        {
                                            id = getDestinationID(IdPoint),
                                            //khungthoigianden = khungthoigianden,
                                            //khungthoigianve = khungthoigianve
                                        }
                                    }
                                });
                            }

                            //sắp xép danh sách điểm để tìm điểm khởi hành
                            TimDiemKhoiHanh(chuyendi);

                            // Lấy khoảng cách từ điểm đầu tiên đến các điểm
                            List<double> distances = new List<double>();
                            foreach (var point in chuyendi)
                            {
                                distances.Add(distancematrix[chuyendi[0].dsdiemcungtoado[0].id][point.dsdiemcungtoado[0].id]);
                            }

                            //sx list theo khoảng cách
                            for (int i = 1; i < chuyendi.Count - 1; i++)
                            {
                                double mindistance = distances[i];
                                int index = i;
                                for (int j = i + 1; j < chuyendi.Count; j++)
                                {
                                    double distance = distances[j];
                                    if (distance < mindistance)
                                    {
                                        mindistance = distance;
                                        index = j;
                                    }
                                }
                                if (index != i)
                                {
                                    var temp = chuyendi[i];
                                    chuyendi[i] = chuyendi[index];
                                    chuyendi[index] = temp;

                                    distances[index] = distances[i];
                                    distances[i] = mindistance;
                                }
                            }

                            var watch1 = System.Diagnostics.Stopwatch.StartNew();

                            //lọc chuyến đi ngược chiều (tách những chuyến đi ngược chiều ra riêng)
                            List<List<Point>> dschuyendicon = new List<List<Point>>() { chuyendi };
                            List<int> OriginIDList = new List<int>();
                            List<int> DestinationIDList = new List<int>();
                            for (int i = 0; i < chuyendi.Count; i++)
                            {
                                for (int j = 0; j < chuyendi[i].dsdiemcungtoado.Count; j++)
                                {
                                    var pointdetail = chuyendi[i].dsdiemcungtoado[j];
                                    if (pointIdIsDestination(pointdetail))
                                    {
                                        bool DaCoOriginID = false;
                                        foreach (var OrgId in OriginIDList)
                                        {
                                            if (getOriginID(pointdetail.id) == OrgId)
                                                DaCoOriginID = true;
                                        }
                                        if (!DaCoOriginID)
                                        {
                                            DestinationIDList.Add(pointdetail.id);
                                            //xóa khỏi chuyendi gốc
                                            dschuyendicon[0][i].dsdiemcungtoado.RemoveAt(j);

                                            //thêm vào list mới
                                            if (dschuyendicon.Count == 1)
                                            {
                                                dschuyendicon.Add(new List<Point>());
                                            }
                                            dschuyendicon[1].Add(new Point
                                            {
                                                location = chuyendi[i].location,
                                                dsdiemcungtoado = new List<point_detail>()
                                                    {
                                                        new point_detail
                                                        {
                                                            id = pointdetail.id,
                                                            khungthoigianden = pointdetail.khungthoigianden,
                                                            khungthoigianve = pointdetail.khungthoigianve
                                                        }
                                                    }
                                            });
                                        }
                                    }
                                    else
                                    {
                                        bool DaCoDestinationID = false;
                                        foreach (var DesID in DestinationIDList)
                                        {
                                            if (getDestinationID(pointdetail.id) == DesID)
                                            {
                                                DaCoDestinationID = true;
                                                //xóa khỏi chuyendi gốc
                                                dschuyendicon[0][i].dsdiemcungtoado.RemoveAt(j);

                                                //thêm vào list mới
                                                dschuyendicon[1].Add(new Point
                                                {
                                                    location = chuyendi[i].location,
                                                    dsdiemcungtoado = new List<point_detail>()
                                                    {
                                                        new point_detail
                                                        {
                                                            id = pointdetail.id
                                                        }
                                                    }
                                                });
                                                break;
                                            }
                                        }
                                        if (!DaCoDestinationID)
                                            OriginIDList.Add(pointdetail.id);
                                    }
                                }
                            }
                            dschuyendicon[0].RemoveAll(item => item.dsdiemcungtoado.Count == 0);
                            if (dschuyendicon.Count == 2)
                                dschuyendicon[1].Reverse();

                            watch1.Stop();
                            var elapsedMs1 = watch1.ElapsedMilliseconds;
                            Console.WriteLine("thời gian thực hiện sắp xếp: {0}", elapsedMs1);

                            //sắp xếp tiếp theo phương hướng
                            foreach (var chuyen in dschuyendicon)
                            {
                                sx(chuyen);

                                Coordinate origin = chuyen[0].location, destination_c1 = chuyen[chuyen.Count - 1].location, destination_c2 = chuyen[chuyen.Count - 2].location;

                                List<Coordinate> waypoints_c1 = new List<Coordinate>(), waypoints_c2 = new List<Coordinate>();
                                for (int i = 1; i < chuyen.Count - 1; i++)
                                {
                                    waypoints_c1.Add(chuyen[i].location);
                                }

                                for (int i = 1; i < chuyen.Count; i++)
                                {
                                    if (i == (chuyen.Count - 2))
                                        continue;
                                    waypoints_c2.Add(chuyen[i].location);
                                }

                                double dist1 = await MapFunction.GetDistanceAsync(origin, destination_c1, waypoints_c1), dist2 = await MapFunction.GetDistanceAsync(origin, destination_c2, waypoints_c2);

                                if (dist1 > dist2)
                                {
                                    int length = chuyen.Count;
                                    var temp = chuyen[length - 2];
                                    chuyen[length - 2] = chuyen[length - 1];
                                    chuyen[length - 1] = temp;
                                }

                                dsChuyenDiTheoDiaDiem.Add(chuyen);
                            }
                        }
                    }
                }

                Console.WriteLine("số kq trước: {0}", DsChuyenDi.Count);

                //phân theo thời gian
                var watch2 = System.Diagnostics.Stopwatch.StartNew();

                Parallel.ForEach(dsChuyenDiTheoDiaDiem, locthoigian);

                watch2.Stop();
                var elapsedMs2 = watch2.Elapsed;
                Console.WriteLine("thời gian phân cụm thời gian: {0}", elapsedMs2);

                //thêm các chuyến còn lại
                for (int id = 0; id < lichdangky.Count; id++)
                {
                    if (status[id] != 0)  // đã xét
                        continue;

                    themchuyendivaodanhsach(DsChuyenDi, getDestinationID(id));
                }
            }

            watchh.Stop();
            var elapsedMss = watchh.Elapsed;
            Console.WriteLine("Tổng thời gian thực hiện thuật toán: {0} (s)", elapsedMss.TotalSeconds);

            double total = 0;
            foreach (var chuyenDi in DsChuyenDi)
            {
                foreach (var d in chuyenDi.dsDiem)
                {
                    foreach (var item in d.dsdiemcungtoado)
                    {
                        if (pointIdIsDestination(item))
                        {
                            total += Math.Abs(gio_stringToDouble(item.thoigianden) - gio_stringToDouble(d.thoigianxeden_luotdi))
                                + Math.Abs(gio_stringToDouble(item.thoigianve) - gio_stringToDouble(d.thoigianxeden_luotve));
                        }
                    }

                }
            }
            double tgchotrungbinh = total / lichdangky.Count;
            Console.WriteLine("Thời gian chờ trung bình: {0} (h)", tgchotrungbinh);

            //SoKq = _DsChuyenDi.Count;
            //ChieuCaoListviewKetQua = sokq * ListviewKetQuaRowHeight;

            DateTime ngaydicongtac = (DateTime)lichdangky.FirstOrDefault().NgayDen;
            for (int i = 0; i < DsChuyenDi.Count; i++)
            {
                var chuyen = DsChuyenDi[i];
                chuyen.DiaChiCuoiCung = chuyen.dsDiem.Last().diachi;
                List<Coordinate> listPoint = new List<Coordinate>();

                foreach (var point in chuyen.dsDiem)
                {
                    listPoint.Add(point.location);
                }

                var property_list = get_ORG_DES_WAY(chuyen.dsDiem);
                Coordinate origin = property_list.origin;
                Coordinate destination = property_list.destination;
                List<Coordinate> waypoints = property_list.waypoints;

                string stringdsdiemdung = MapFunction.Encode(listPoint);
                var waypoints2 = new List<Coordinate>(waypoints);
                waypoints2.Reverse();
                string stringtuyenduongdi = await MapFunction.GetPolyline(origin, destination, waypoints);
                string stringtuyenduongve = await MapFunction.GetPolyline(destination, origin, waypoints2);

                var listChitietnguoidi = new List<Chitietnguoidi>();
                foreach (var point in chuyen.dsDiem)
                {
                    foreach (var point_Detail in point.dsdiemcungtoado)
                    {
                        if (pointIdIsOrigrin(point_Detail))
                        {
                            listChitietnguoidi.Add(new Chitietnguoidi
                            {
                                DangKyLichChiTietId = point_Detail.DangKyLichChiTietId,
                                DangKyLichId = point_Detail.DangKyLichId,
                                NoiDi = point_Detail.NoiDi,
                                NoiDen = point_Detail.NoiDen,
                                GioDi = point.thoigianxeden_luotdi
                            });
                        }
                    }
                }
                DsChuyenDi[i].listChitietnguoidi = listChitietnguoidi;
                DsDieuXe.Add(new LichDieuXe
                {
                    NoiDi = chuyen.dsDiem[0].diachi,
                    NoiDen = chuyen.DiaChiCuoiCung,
                    GioDi = chuyen.dsDiem[0].thoigianxeden_luotdi,
                    GioVe = chuyen.dsDiem[0].thoigianxeden_luotve,
                    TrangThai = 0,
                    TuyenDuongDi = stringtuyenduongdi,
                    TuyenDuongVe = stringtuyenduongve,
                    BienKiemSoat = chuyen.xedi.BienSoXe,
                    NgayDi = ngaydicongtac,
                    SoNguoi = chuyen.tongsonguoi,
                    DsDiemDung = stringdsdiemdung,
                    listChitietnguoidi = listChitietnguoidi,
                    //dsTaixe = new List<LienHeUser>(dstaixe),
                    //IndexDieuXe = i + 1
                });
            }

            json1 = JsonConvert.SerializeObject(DsChuyenDi);
            json2 = JsonConvert.SerializeObject(DsDieuXe);

            Console.WriteLine("Tổng quãng đường đi: {0} (km)", MapFunction.tongquangduong / 1000);

            Console.WriteLine("số kq sau: {0}", DsChuyenDi.Count);
            System.Console.WriteLine("tổng số request: " + MapFunction.CountRequest);

            void TimDiemKhoiHanh(List<Point> DsDiem)
            {
                if (DsDiem.Count <= 2)
                    return;
                int datacount = lichdangky.Count;

                //giả sử các điểm tại vị trí i = 1, 2, 3 là p1->p2->p3
                Coordinate p1 = new Coordinate(DsDiem[0].location.Latitude, DsDiem[0].location.Longitude);
                Coordinate p2 = new Coordinate(DsDiem[1].location.Latitude, DsDiem[1].location.Longitude);
                Coordinate p3 = new Coordinate(DsDiem[2].location.Latitude, DsDiem[2].location.Longitude);

                Vector vector12 = MapFunction.layVector(p1, p2), vector13 = MapFunction.layVector(p1, p3);
                Vector vector21 = MapFunction.layVector(p2, p1), vector23 = MapFunction.layVector(p2, p3);
                Vector vector31 = MapFunction.layVector(p3, p1), vector32 = MapFunction.layVector(p3, p2);
                double goc1 = MapFunction.timgoc(vector12, vector13), goc2 = MapFunction.timgoc(vector21, vector23), goc3 = MapFunction.timgoc(vector31, vector32);
                if (goc1 > goc2 && goc1 >= goc3)
                {
                    DsDiem.Reverse(0, 2);
                }
                else if (goc3 > goc2)
                {
                    DsDiem.Reverse(1, 2);
                }

                for (int i = 3; i < DsDiem.Count; i++)
                {
                    if (pointIdIsDestination(DsDiem[i].dsdiemcungtoado[0]))
                        break;
                    double kcdendiemdau = distancematrix[DsDiem[0].dsdiemcungtoado[0].id][DsDiem[i].dsdiemcungtoado[0].id];
                    double kcdendiemcuoi = distancematrix[DsDiem[i - 1].dsdiemcungtoado[0].id][DsDiem[i].dsdiemcungtoado[0].id];
                    if (kcdendiemdau < kcdendiemcuoi)
                    {
                        DsDiem.Reverse(0, i);
                    }
                }
            }

            void sx(List<Point> chuyendi)
            {
                System.Console.WriteLine("hàm sắp xếp");
                int sodiem = chuyendi.Count;
                int datacount = lichdangky.Count;
                for (int i = 0; i <= sodiem - 3; i++)
                {
                    //giả sử các điểm tại vị trí i = 1, 2, 3 là p1->p2->p3
                    Coordinate p1 = new Coordinate(chuyendi[i].location.Latitude, chuyendi[i].location.Longitude);
                    Coordinate p2 = new Coordinate(chuyendi[i + 1].location.Latitude, chuyendi[i + 1].location.Longitude);
                    Coordinate p3 = new Coordinate(chuyendi[i + 2].location.Latitude, chuyendi[i + 2].location.Longitude);
                    if (pointIdIsOrigrin(chuyendi[i + 1].dsdiemcungtoado[0]))
                        continue;
                    if (!thutu3diem(p1, p2, p3))
                    {   //nếu góc tại điểm p3 lớn hơn góc tại điểm p2 thì p3 nằm giữa p1 vs p2
                        if (i == sodiem - 3)
                        {
                            var temp = chuyendi[i + 1];
                            chuyendi[i + 1] = chuyendi[i + 2];
                            chuyendi[i + 2] = temp;
                            break;
                        }
                        //có 2 trường hợp: 1->3->2 hoặc 2->3->1
                        Coordinate p4 = new Coordinate(chuyendi[i + 3].location.Latitude, chuyendi[i + 3].location.Longitude);
                        //TH 1->3->2, xét p3, p2, p4
                        Vector vector23 = MapFunction.layVector(p2, p3), vector24 = MapFunction.layVector(p2, p4);
                        double anpha = MapFunction.timgoc(vector23, vector24);
                        //TH 2->3->1, xét p3, p1, p4
                        Vector vector13 = MapFunction.layVector(p1, p3), vector14 = MapFunction.layVector(p1, p4);
                        double beta = MapFunction.timgoc(vector13, vector14);
                        if (anpha >= beta)
                        {//chọn TH1
                            var temp = chuyendi[i + 1];
                            chuyendi[i + 1] = chuyendi[i + 2];
                            chuyendi[i + 2] = temp;
                        }
                        else
                        {  //TH2
                            var temp = chuyendi[i];
                            chuyendi[i] = chuyendi[i + 1];
                            chuyendi[i + 1] = chuyendi[i + 2];
                            chuyendi[i + 2] = temp;
                        }
                    }
                }
            }

            bool thutu3diem(Coordinate p1, Coordinate p2, Coordinate p3)
            {
                Vector vector21 = MapFunction.layVector(p2, p1), vector23 = MapFunction.layVector(p2, p3);
                double anpha = MapFunction.timgoc(vector21, vector23); //góc tại điểm p2
                Vector vector31 = MapFunction.layVector(p3, p1), vector32 = MapFunction.layVector(p3, p2);
                double beta = MapFunction.timgoc(vector31, vector32); //góc tại điểm p3
                if (beta > anpha)
                    return false;
                else
                    return true;
            }

            int TimGocLonNhat(Coordinate p1, Coordinate p2, Coordinate p3)
            {
                double goc1 = MapFunction.timgoc(MapFunction.layVector(p1, p2), MapFunction.layVector(p1, p3));
                double goc2 = MapFunction.timgoc(MapFunction.layVector(p2, p1), MapFunction.layVector(p2, p3));
                double goc3 = MapFunction.timgoc(MapFunction.layVector(p3, p1), MapFunction.layVector(p3, p2));
                return goc1 >= goc2 ? (goc1 >= goc3 ? 1 : 3) : (goc2 >= goc3 ? 2 : 3);
            }

            double[] CoordinatesToArray(string coor)
            {
                string[] result = { "", "" };
                char[] charArray = coor.ToCharArray();
                var k = 0;
                for (var i = 0; i < charArray.Length; i++)
                {
                    if (charArray[i] != ',' && charArray[i] != ' ')
                        result[k] += charArray[i];
                    else
                        k = 1;
                }
                return new double[] { Convert.ToDouble(result[0]), Convert.ToDouble(result[1]) };
            }

            //thời gian
            void locthoigian(List<Point> chuyendibandau)
            {
                List<ChuyenDi> dschuyendi = new List<ChuyenDi>();
                if (chuyendibandau.Count < 2)
                    return;

                foreach (Point point in chuyendibandau)
                {
                    if (pointContaintAllOrigrin(point))
                        continue;
                    foreach (var point_Detail in point.dsdiemcungtoado)
                    {
                        if (pointIdIsOrigrin(point_Detail))
                            continue;
                        themchuyendivaodanhsach(dschuyendi, point_Detail.id, chuyendibandau);
                    }
                }

                foreach (var item in dschuyendi)
                    DsChuyenDi.Add(item);
            }

            void themchuyendivaodanhsach(List<ChuyenDi> dschuyendi, int DestinationID, List<Point> chuyendibandau = null)    //chuyendibandau = null -> chuyến đi đang thêm chưa được gom cụm
            {
                if (DestinationID < lichdangky.Count)
                    throw new ArgumentException("Giá trị đối số không hợp lệ", "DestinationID");

                bool themchuyendithanhcong = false;

                int OriginID = getOriginID(DestinationID);
                int DangKyLichChiTietId = lichdangky[OriginID].DangKyLichChiTietId;
                int DangKyLichId = lichdangky[OriginID].DangKyLichId.Value;
                string NoiDi = lichdangky[OriginID].NoiDi;
                string NoiDen = lichdangky[OriginID].NoiDen;
                Coordinate OrgLocation = Original_point[OriginID];
                Coordinate DesLocation = Destination_point[OriginID];
                int songuoidi = lichdangky[OriginID].SoNguoi.Value;
                TimeSpan GioDen = lichdangky[OriginID].GioDen.Value;
                TimeSpan GioVe = lichdangky[OriginID].GioVe.Value;
                var khungthoigian = laykhungthoigian(GioDen, GioVe, durationmatrix[OriginID][DestinationID]);
                TimeSpan[] khungthoigianden = khungthoigian[0];
                TimeSpan[] khungthoigianve = khungthoigian[1];    //fff

                Point orgPoint = new Point();
                orgPoint.location = OrgLocation; //
                orgPoint.diachi = NoiDi;
                orgPoint.dsdiemcungtoado = new List<point_detail>()
                {
                    new point_detail
                    {
                        id = OriginID,
                        songuoi = songuoidi,
                        DangKyLichChiTietId = DangKyLichChiTietId,
                        DangKyLichId = DangKyLichId,
                        NoiDen = NoiDen,
                        NoiDi = NoiDi
                    }
                };

                Point desPoint = new Point();
                desPoint.location = DesLocation;
                desPoint.diachi = NoiDen;
                desPoint.thoigianxeden_luotdi = khungthoigianden[1];
                desPoint.thoigianxeden_luotve = khungthoigianve[0];
                desPoint.dsdiemcungtoado = new List<point_detail>()
                {
                    new point_detail
                    {
                        id = DestinationID,
                        songuoi = songuoidi,
                        thoigianden = GioDen,
                        thoigianve = GioVe,
                        khungthoigianden = khungthoigianden,
                        khungthoigianve = khungthoigianve,
                        DangKyLichChiTietId = DangKyLichChiTietId,
                        DangKyLichId = DangKyLichId,
                        NoiDen = NoiDen,
                        NoiDi = NoiDi
                    }
                };

                var ChuyenDiThoaDk = new { vitrichuyendi = 0, xedi = new Xe_v2(), chuyendimoi = new ChuyenDi(), thoigianlangphi = 0.0 };    //vị trí của chuyendi, đoạn đường dài thêm, thời gian dư thừa thêm
                var dsChuyenDiThoaDk = new[] { ChuyenDiThoaDk }.ToList();
                dsChuyenDiThoaDk.Clear();

                for (int index = 0; index < dschuyendi.Count; index++)    ////xét từng chuyến đi
                {
                    ChuyenDi chuyendi = dschuyendi[index];
                    Xe_v2 xehientai = null;
                    foreach (var xe in dsXe)
                    {
                        if (xe.ThongTinXe.Equals(chuyendi.xedi))
                        {
                            xehientai = xe;
                            break;
                        }
                    }
                    if (xehientai == null)
                    {
                        Console.WriteLine("xe not found");
                        continue;
                    }
                    ChuyenDi copyOfChuyendi = new ChuyenDi
                    {
                        dsDiem = new List<Point>(chuyendi.dsDiem),
                        tongsonguoi = chuyendi.tongsonguoi,
                        xedi = chuyendi.xedi
                    };
                    int vitriOrgTrongChuyenDi = 0;
                    int vitriDesTrongChuyenDi = 0;

                    TimeSpan tongthoigian_truoc = new TimeSpan(0);
                    TimeSpan tongthoigian_sau = new TimeSpan(0);

                    for (int i = 0; i < copyOfChuyendi.dsDiem.Count; i++)
                    {
                        var tgxd_luotdi = copyOfChuyendi.dsDiem[i].thoigianxeden_luotdi;
                        var tgxd_luotve = copyOfChuyendi.dsDiem[i].thoigianxeden_luotve;

                        foreach (var pointdetail in copyOfChuyendi.dsDiem[i].dsdiemcungtoado)
                        {
                            if (pointIdIsOrigrin(pointdetail))
                            {
                                tongthoigian_truoc += (lichdangky[pointdetail.id].GioDen.Value - tgxd_luotdi).Duration();
                                tongthoigian_truoc += (tgxd_luotve - lichdangky[pointdetail.id].GioVe.Value).Duration();
                            }
                        }
                    }

                    //double tongthoigianduthuathem = 0;
                    bool DaCoToaDoOrigin = false;
                    bool DaCoToaDoDestination = false;
                    bool OrgCuoiDanhSach = false;
                    bool DesCuoiDanhSach = false;
                    //thêm điểm origin và destination vào list
                    if (chuyendibandau != null)  //thêm điểm dựa vào chuyến đi ban đầu
                    {
                        int Index = 0;
                        bool OriginAdded = false, DestinationAdded = false;
                        for (int i = 0; i < chuyendibandau.Count; i++)
                        {
                            if (OriginAdded && DestinationAdded)
                                break;
                            bool issame = false;
                            if (Index < copyOfChuyendi.dsDiem.Count && chuyendibandau[i].location.IsSame(copyOfChuyendi.dsDiem[Index].location))
                                issame = true;
                            foreach (var item in chuyendibandau[i].dsdiemcungtoado)
                            {
                                //điểm Origin
                                if (item.id == OriginID)
                                {
                                    vitriOrgTrongChuyenDi = Index;
                                    if (issame)
                                    {   //location đã trùng -> thêm id vào dsdiemcungtoado
                                        DaCoToaDoOrigin = true;
                                        copyOfChuyendi.dsDiem[Index].dsdiemcungtoado.Add(orgPoint.dsdiemcungtoado[0]);
                                        OriginAdded = true;
                                    }
                                    else
                                    {   //chèn điểm vào trước vị trí Index
                                        if (Index == copyOfChuyendi.dsDiem.Count)
                                        {
                                            OrgCuoiDanhSach = true;
                                            copyOfChuyendi.dsDiem.Add(orgPoint);
                                        }
                                        else
                                            copyOfChuyendi.dsDiem.Insert(Index, orgPoint);
                                        //doanduongdaithem += Index == 0 ? distancematrix[OriginID][chuyendi.dsDiem[0].dsdiemcungtoado[0].id] : 
                                        //                                 distancematrix[Index-1][Index] + distancematrix[Index][Index+1] - distancematrix[Index-1][Index+1];
                                        Index++;
                                        OriginAdded = true;
                                    }
                                }
                                //điểm Destination
                                if (item.id == DestinationID)
                                {
                                    vitriDesTrongChuyenDi = Index;
                                    if (issame)
                                    {   //location đã trùng -> thêm id vào dsdiemcungtoado
                                        DaCoToaDoDestination = true;
                                        copyOfChuyendi.dsDiem[Index].dsdiemcungtoado.Add(desPoint.dsdiemcungtoado[0]);
                                        if (Index == copyOfChuyendi.dsDiem.Count - 1)
                                            DesCuoiDanhSach = true;
                                        DestinationAdded = true;
                                    }
                                    else
                                    {   //chèn điểm vào trước vị trí Index
                                        if (Index == copyOfChuyendi.dsDiem.Count)
                                        {
                                            copyOfChuyendi.dsDiem.Add(desPoint);
                                            DesCuoiDanhSach = true;
                                        }
                                        else
                                            copyOfChuyendi.dsDiem.Insert(Index, desPoint);
                                        //check
                                        //doanduongdaithem += Index == copyOfChuyendi.dsDiem.Count ? distancematrix[chuyendi.dsDiem[Index - 2].dsdiemcungtoado[0].id][DestinationID] :
                                        //                                 distancematrix[Index - 1][Index] + distancematrix[Index][Index + 1] - distancematrix[Index - 1][Index + 1];
                                        Index++;
                                        DestinationAdded = true;
                                    }
                                }
                            }
                            if (issame && (!OriginAdded || !DestinationAdded))
                                Index++;
                        }

                    }
                    else     // thêm điểm thủ công (khi thêm phải xét khía cạnh địa điểm, phương hướng)
                    {
                        bool diNguocChieu = false;
                        for (int i = 0; i < copyOfChuyendi.dsDiem.Count; i++)
                        {
                            if (orgPoint.location.IsSame(copyOfChuyendi.dsDiem[i].location))
                            {
                                DaCoToaDoOrigin = true;
                                vitriOrgTrongChuyenDi = i;
                                copyOfChuyendi.dsDiem[i].dsdiemcungtoado.Add(orgPoint.dsdiemcungtoado.FirstOrDefault());
                                break;
                            }
                            if (orgPoint.location.IsSame(copyOfChuyendi.dsDiem[i + 1].location))
                                continue;
                            int vitricogoclonnhat = TimGocLonNhat(copyOfChuyendi.dsDiem[i].location, orgPoint.location, copyOfChuyendi.dsDiem[i + 1].location);
                            if (vitricogoclonnhat == 3)
                            {
                                if (i + 1 == copyOfChuyendi.dsDiem.Count - 1)
                                {
                                    OrgCuoiDanhSach = true;
                                    copyOfChuyendi.dsDiem.Add(orgPoint);
                                    vitriOrgTrongChuyenDi = i + 2;
                                    break;
                                }
                                continue;
                            }
                            if (vitricogoclonnhat == 1)
                            {
                                vitriOrgTrongChuyenDi = i;
                                copyOfChuyendi.dsDiem.Insert(i, orgPoint);
                                break;
                            }
                            if (vitricogoclonnhat == 2)
                            {
                                vitriOrgTrongChuyenDi = i + 1;
                                copyOfChuyendi.dsDiem.Insert(i + 1, orgPoint);
                                break;
                            }
                        }
                        for (int i = 0; i < copyOfChuyendi.dsDiem.Count; i++)
                        {
                            if (desPoint.location.IsSame(copyOfChuyendi.dsDiem[i].location))
                            {
                                if (i < vitriOrgTrongChuyenDi)
                                {
                                    diNguocChieu = true;
                                    break;
                                }
                                DaCoToaDoDestination = true;
                                vitriDesTrongChuyenDi = i;
                                copyOfChuyendi.dsDiem[i].dsdiemcungtoado.Add(desPoint.dsdiemcungtoado.FirstOrDefault());

                                if (i == copyOfChuyendi.dsDiem.Count - 1)
                                    DesCuoiDanhSach = true;
                                break;
                            }
                            if (desPoint.location.IsSame(copyOfChuyendi.dsDiem[i + 1].location))
                                continue;
                            int vitricogoclonnhat = TimGocLonNhat(copyOfChuyendi.dsDiem[i].location, desPoint.location, copyOfChuyendi.dsDiem[i + 1].location);
                            if (vitricogoclonnhat == 3)
                            {
                                if (i + 1 == copyOfChuyendi.dsDiem.Count - 1)
                                {
                                    copyOfChuyendi.dsDiem.Add(desPoint);
                                    DesCuoiDanhSach = true;
                                    vitriDesTrongChuyenDi = copyOfChuyendi.dsDiem.Count - 1;
                                    break;
                                }
                                continue;
                            }
                            if (vitricogoclonnhat == 1)
                            {
                                if (i <= vitriOrgTrongChuyenDi)
                                {
                                    diNguocChieu = true;
                                    break;
                                }
                                copyOfChuyendi.dsDiem.Insert(i, desPoint);
                                vitriDesTrongChuyenDi = i;
                                break;
                            }
                            if (vitricogoclonnhat == 2)
                            {
                                if (i + 1 < vitriOrgTrongChuyenDi)
                                {
                                    diNguocChieu = true;
                                    break;
                                }
                                copyOfChuyendi.dsDiem.Insert(i + 1, desPoint);
                                vitriDesTrongChuyenDi = i + 1;
                                break;
                            }
                        }
                        if (diNguocChieu)
                        {
                            Console.WriteLine("đi ngc chiều.");
                            continue;
                        }
                    }

                    int sodiem = copyOfChuyendi.dsDiem.Count;

                    ////lấy thời gian đi của mọi người đi xe
                    //double tgtruoc = 0, tgsau = 0;    // tổng thời gian trên chuyến đi của tất cả nhân viên
                    //int songuoitrenxe_truoc = 0, songuoitrenxe_sau = 0;
                    //int iddiemtruoc = 0;

                    //for (int ind = 0; ind < sodiem; ind++)
                    //{
                    //    if (ind == vitriOrgTrongChuyenDi || ind == vitriDesTrongChuyenDi)
                    //    {
                    //        if (copyOfChuyendi.dsDiem[ind].dsdiemcungtoado.Count == 1)    //điểm chứa origin chưa tồn tại từ trước
                    //        {
                    //            iddiemtruoc = tgtruoc == 0 ? copyOfChuyendi.dsDiem[ind + 1].dsdiemcungtoado[0].id : copyOfChuyendi.dsDiem[ind - 1].dsdiemcungtoado[0].id;
                    //            tgsau += ind == 0 ? 0 : songuoitrenxe_sau * durationmatrix[copyOfChuyendi.dsDiem[ind - 1].dsdiemcungtoado[0].id][copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id];
                    //            songuoitrenxe_sau += ind == vitriOrgTrongChuyenDi ? 1 : -1;
                    //        }
                    //        else
                    //        {
                    //            tgtruoc += ind == 0 ? 0 : songuoitrenxe_truoc * durationmatrix[iddiemtruoc][copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id];
                    //            tgsau += ind == 0 ? 0 : songuoitrenxe_sau * durationmatrix[copyOfChuyendi.dsDiem[ind - 1].dsdiemcungtoado[0].id][copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id];
                    //            foreach (var pointdetail in copyOfChuyendi.dsDiem[ind].dsdiemcungtoado)
                    //            {
                    //                songuoitrenxe_truoc += pointIdIsOrigrin(pointdetail) ? (pointdetail.id == OriginID ? 0 : 1) : (pointdetail.id == DestinationID ? 0 : -1);
                    //                songuoitrenxe_sau += pointIdIsOrigrin(pointdetail) ? 1 : -1;
                    //            }

                    //            iddiemtruoc = copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        tgtruoc += ind == 0 ? 0 : songuoitrenxe_truoc * durationmatrix[iddiemtruoc][copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id];
                    //        tgsau += ind == 0 ? 0 : songuoitrenxe_sau * durationmatrix[copyOfChuyendi.dsDiem[ind - 1].dsdiemcungtoado[0].id][copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id];
                    //        foreach (var pointdetail in copyOfChuyendi.dsDiem[ind].dsdiemcungtoado)
                    //        {
                    //            songuoitrenxe_truoc += pointIdIsOrigrin(pointdetail) ? 1 : -1;
                    //            songuoitrenxe_sau += pointIdIsOrigrin(pointdetail) ? 1 : -1;
                    //        }

                    //        iddiemtruoc = copyOfChuyendi.dsDiem[ind].dsdiemcungtoado[0].id;
                    //    }

                    //    //foreach (var pointdetail in copyOfChuyendi.dsDiem[ind].dsdiemcungtoado)
                    //    //{
                    //    //    foreach (var item in dsnguoichuaden)
                    //    //    {
                    //    //        dsthoigiandi[item] += durationmatrix[ind - 1][ind];
                    //    //    }
                    //    //    if (pointIdIsOrigrin(pointdetail))  //origin
                    //    //    {
                    //    //        dsnguoichuaden.Add(pointdetail.id);
                    //    //        dsthoigiandi.Add(pointdetail.id, 0.0);
                    //    //    }
                    //    //    else    //destination
                    //    //    {
                    //    //        for (int i = 0; i < dsnguoichuaden.Count; i++)
                    //    //        {
                    //    //            if (dsnguoichuaden[i] == getOriginID(pointdetail.id))
                    //    //                dsnguoichuaden.RemoveAt(i);
                    //    //        }
                    //    //    }
                    //    //}

                    //}
                    //Console.WriteLine("tg trước: {0}, tg sau: {1}", tgtruoc, tgsau);
                    //double delta = tgsau - tgtruoc;

                    bool dieukien1 = true;  //dieukien1: chuyến đi này khi thêm vào không ảnh hưởng giờ giấc lượt đi của những chuyến khác trong list
                    bool dieukien2 = true;  //dieukien2: chuyến đi này khi thêm vào không ảnh hưởng giờ giấc lượt về của những chuyến khác trong list

                    var TaskList = new List<Task>();

                    //xét điều kiện 1       //KIỂM TRA LẠI ĐK 1

                    int idTruocOrg = vitriOrgTrongChuyenDi == 0 ? -1 : copyOfChuyendi.dsDiem[vitriOrgTrongChuyenDi - 1].dsdiemcungtoado[0].id;
                    int idSauOrg = copyOfChuyendi.dsDiem[vitriOrgTrongChuyenDi + 1].dsdiemcungtoado[0].id;
                    int idTruocDes = copyOfChuyendi.dsDiem[vitriDesTrongChuyenDi - 1].dsdiemcungtoado[0].id;
                    int idSauDes = vitriDesTrongChuyenDi == sodiem - 1 ? -1 : copyOfChuyendi.dsDiem[vitriDesTrongChuyenDi + 1].dsdiemcungtoado[0].id;
                    double thoigianthembot_Org = (DaCoToaDoOrigin || vitriOrgTrongChuyenDi == 0 || vitriOrgTrongChuyenDi == sodiem - 2) ? 0 : (double)(duration_in_traffic_matrix[idTruocOrg][OriginID] + duration_in_traffic_matrix[OriginID][idSauOrg] - duration_in_traffic_matrix[idTruocOrg][idSauOrg]) / 60;
                    double thoigianthembot_Des = (DaCoToaDoDestination || vitriDesTrongChuyenDi == 1 || vitriDesTrongChuyenDi == sodiem - 1) ? 0 : (double)(duration_in_traffic_matrix[idTruocDes][DestinationID] + duration_in_traffic_matrix[DestinationID][idSauDes] - duration_in_traffic_matrix[idTruocDes][idSauDes]) / 60;

                    bool timthayvitriOrg = false;

                    TimeSpan tgxd = new TimeSpan(0), mintime = new TimeSpan(0), maxtime = new TimeSpan(0);
                    int vitri = -1;
                    for (int i = 0; i < sodiem; i++)
                    {
                        bool breakRequest = false;
                        if (i == vitriOrgTrongChuyenDi)
                        {
                            timthayvitriOrg = true;
                            continue;   //bỏ điểm hiện tại, xét điểm tiếp theo
                        }

                        if (i < vitriDesTrongChuyenDi)
                        {
                            foreach (var pointdetail in copyOfChuyendi.dsDiem[i].dsdiemcungtoado)
                            {
                                if (pointIdIsOrigrin(pointdetail))
                                    continue;
                                tgxd = copyOfChuyendi.dsDiem[i].thoigianxeden_luotve;
                                mintime = pointdetail.khungthoigianve[0];
                                maxtime = pointdetail.khungthoigianve[1];

                                vitri = i;
                            }
                        }

                        //if (i == vitriDesTrongChuyenDi)
                        //{
                        //    timthayvitriDes = true;
                        //    continue;   //bỏ điểm hiện tại, xét điểm tiếp theo
                        //}
                        //if (timthayvitriDes)
                        //{
                        //    continue;
                        //}
                        if (!timthayvitriOrg)
                        {
                            //cập nhật thời gian xe đến (lượt về) (vì khi thêm điểm origin làm thời gian về của các điểm destination trước điểm origin này bị trễ hoặc sớm)
                            var tgxd_truockhithem = copyOfChuyendi.dsDiem[i].thoigianxeden_luotve;
                            var tgxd_saukhithem = tgxd_truockhithem.Add(TimeSpan.FromMinutes(thoigianthembot_Org + thoigianthembot_Des));
                            copyOfChuyendi.dsDiem[i].thoigianxeden_luotve = tgxd_saukhithem;
                        }
                        else
                        {
                            //if (DaCoToaDoOrigin)
                            //    continue;
                            //xét đk giờ lượt đi
                            foreach (var pointdetail in copyOfChuyendi.dsDiem[i].dsdiemcungtoado)
                            {
                                /*dán đk  "&& pointdetail.id != DestinationID"  */  // đã xử lý
                                if (pointIdIsDestination(pointdetail))  //tìm kiếm điểm Destination trong các điểm nằm sau vị trí điểm Origin
                                {

                                    // nếu sau khi thêm mà tgxd điểm này thấp hơn thời gian dưới thì tgxuatphat sẽ sớm hơn để đáp ứng nó
                                    // nếu tgxd điểm này lớn hơn thời gian trên thì tgxuatphat sẽ phải muộn hơn
                                    // nếu không thì không thay đổi thời gian xuất phát
                                    TimeSpan tgxuatphatThaydoi1 = new TimeSpan(0);
                                    if (DesCuoiDanhSach)
                                    {
                                        TimeSpan tgxd_diemcuoi = copyOfChuyendi.dsDiem[sodiem - 1].dsdiemcungtoado.Count > 1 ?  //Des nằm cuối và điểm này đã có từ trước
                                            copyOfChuyendi.dsDiem[sodiem - 1].thoigianxeden_luotdi
                                            : (vitriOrgTrongChuyenDi == vitriDesTrongChuyenDi - 1 && !DaCoToaDoOrigin) ?        //Điểm des và org chưa có trước và org nằm liền trước des
                                                copyOfChuyendi.dsDiem[sodiem - 3].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[sodiem - 3].dsdiemcungtoado[0].id][OriginID]) / 60 + (double)(duration_in_traffic_matrix[OriginID][DestinationID]) / 60))
                                                : copyOfChuyendi.dsDiem[sodiem - 2].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[sodiem - 2].dsdiemcungtoado[0].id][DestinationID]) / 60));

                                        tgxuatphatThaydoi1 = khungthoigianden[0] > tgxd_diemcuoi ? khungthoigianden[0] - tgxd_diemcuoi
                                                : (khungthoigianden[1] < tgxd_diemcuoi ? khungthoigianden[1] - tgxd_diemcuoi : new TimeSpan(0));
                                    }
                                    else
                                    {
                                        TimeSpan tgxd_truockhithem = copyOfChuyendi.dsDiem[i].thoigianxeden_luotdi;
                                        TimeSpan tgxd_saukhithem = tgxd_truockhithem.Add(TimeSpan.FromMinutes(thoigianthembot_Org));
                                        if (i > vitriDesTrongChuyenDi)
                                            tgxd_saukhithem = tgxd_saukhithem.Add(TimeSpan.FromMinutes(thoigianthembot_Des));

                                        TimeSpan thoigianduoi = pointdetail.khungthoigianden[0];
                                        TimeSpan thoigiantren = pointdetail.khungthoigianden[1];

                                        tgxuatphatThaydoi1 = thoigianduoi > tgxd_saukhithem ? thoigianduoi - tgxd_saukhithem : (thoigiantren < tgxd_saukhithem ? thoigiantren - tgxd_saukhithem : new TimeSpan(0));
                                    }

                                    double thoigianthembot1 = 0;
                                    double thoigianthembot2 = thoigianthembot_Org + thoigianthembot_Des;

                                    TimeSpan thoigianxecho = khungthoigianve[1] - khungthoigianve[0]; //thoigianxecho bằng thời gian lệch tối đa của chuyến đi
                                    tgxd.Add(TimeSpan.FromMinutes(thoigianthembot_Des));
                                    tgxd.Add(TimeSpan.FromMinutes(vitriOrgTrongChuyenDi == sodiem - 2 ? 0 : vitri < vitriOrgTrongChuyenDi ? thoigianthembot_Org : 0));

                                    TimeSpan tgxuatphatThaydoi2 = new TimeSpan(0);
                                    if (DesCuoiDanhSach)
                                    {
                                        TimeSpan tgxd_diemcuoi = copyOfChuyendi.dsDiem[sodiem - 1].dsdiemcungtoado.Count > 1 ? //điểm destination đã tồn tại
                                            copyOfChuyendi.dsDiem[sodiem - 1].thoigianxeden_luotve
                                            : (vitriOrgTrongChuyenDi == vitriDesTrongChuyenDi - 1 && !DaCoToaDoOrigin) ?
                                                copyOfChuyendi.dsDiem[sodiem - 3].thoigianxeden_luotve.Add(TimeSpan.FromMinutes(-(double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[sodiem - 3].dsdiemcungtoado[0].id][OriginID]) / 60 - (double)(duration_in_traffic_matrix[OriginID][DestinationID]) / 60))
                                                : copyOfChuyendi.dsDiem[sodiem - 2].thoigianxeden_luotve.Add(TimeSpan.FromMinutes(-(double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[sodiem - 2].dsdiemcungtoado[0].id][DestinationID]) / 60));

                                        tgxuatphatThaydoi2 = tgxd_diemcuoi < khungthoigianve[0] ? khungthoigianve[0] - tgxd_diemcuoi : new TimeSpan(0);
                                        if (tgxuatphatThaydoi2.TotalMinutes == 0)
                                            if (copyOfChuyendi.dsDiem[sodiem - 1].dsdiemcungtoado.Count == 1)
                                                copyOfChuyendi.dsDiem[sodiem - 2].thoigianxeden_luotve = khungthoigianve[0].Add(TimeSpan.FromMinutes((double)duration_in_traffic_matrix[DestinationID][copyOfChuyendi.dsDiem[sodiem - 2].dsdiemcungtoado[0].id] / 60));
                                        //else
                                        //    copyOfChuyendi.dsDiem[sodiem - 1].thoigianxeden_luotve = khungthoigianve[0];
                                    }
                                    else
                                    {
                                        if (vitri == -1)     //destination này là des cuối cùng trong list (theo hướng đi về)
                                        {
                                            TimeSpan tgxd_diemcuoi = copyOfChuyendi.dsDiem[1].dsdiemcungtoado.Count > 1 ? //điểm destination đã tồn tại
                                                copyOfChuyendi.dsDiem[1].thoigianxeden_luotve
                                                : copyOfChuyendi.dsDiem[2].thoigianxeden_luotve.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[DestinationID][copyOfChuyendi.dsDiem[2].dsdiemcungtoado[0].id]) / 60));

                                            tgxuatphatThaydoi2 = tgxd_diemcuoi > khungthoigianve[1] ? khungthoigianve[1] - tgxd_diemcuoi : new TimeSpan(0);
                                        }
                                        else
                                            tgxuatphatThaydoi2 = mintime > tgxd ? mintime - tgxd : (maxtime < tgxd ? maxtime - tgxd : new TimeSpan(0));
                                    }

                                    TimeSpan tgdagiulai = new TimeSpan(0);
                                    TimeSpan tgxpthaydoiconlai = tgxuatphatThaydoi2;
                                    int taskcapnhatgiove = 0;
                                    for (int j = 0; j < sodiem; j++)  //lặp lại chuyến đi để xét dk1 và lưu task cho dk2
                                    {
                                        if ((j == vitriOrgTrongChuyenDi && !DaCoToaDoOrigin) || (j == vitriDesTrongChuyenDi && !DaCoToaDoDestination))
                                        {
                                            if (j == vitriOrgTrongChuyenDi)
                                            {
                                                TimeSpan tgxd_luotdi = j != 0 ? copyOfChuyendi.dsDiem[j - 1].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[j - 1].dsdiemcungtoado[0].id][OriginID]) / 60)) :
                                                    (j + 1 == vitriDesTrongChuyenDi && !DaCoToaDoDestination) ?
                                                        copyOfChuyendi.dsDiem[j + 2].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes(-(double)(duration_in_traffic_matrix[DestinationID][copyOfChuyendi.dsDiem[i + 2].dsdiemcungtoado[0].id] + duration_in_traffic_matrix[OriginID][DestinationID]) / 60))
                                                        : copyOfChuyendi.dsDiem[j + 1].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes(-(double)(duration_in_traffic_matrix[OriginID][copyOfChuyendi.dsDiem[j + 1].dsdiemcungtoado[0].id]) / 60));
                                                copyOfChuyendi.dsDiem[j].thoigianxeden_luotdi = tgxd_luotdi;

                                                TimeSpan tgxd_luotve = j == sodiem - 2 ?
                                                    copyOfChuyendi.dsDiem[sodiem - 3].thoigianxeden_luotve.Add(TimeSpan.FromMinutes(-(double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[sodiem - 3].dsdiemcungtoado[0].id][OriginID]) / 60))
                                                    : copyOfChuyendi.dsDiem[j + 1].thoigianxeden_luotve.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[j + 1].dsdiemcungtoado[0].id][OriginID]) / 60));
                                                copyOfChuyendi.dsDiem[j].thoigianxeden_luotve = tgxd_luotve;

                                                thoigianthembot2 -= thoigianthembot_Org;

                                                continue;
                                            }
                                            if (j == vitriDesTrongChuyenDi)
                                            {
                                                TimeSpan tgxd_luotdi = copyOfChuyendi.dsDiem[j - 1].thoigianxeden_luotdi.Add(TimeSpan.FromMinutes((double)(duration_in_traffic_matrix[copyOfChuyendi.dsDiem[j - 1].dsdiemcungtoado[0].id][DestinationID]) / 60));

                                                copyOfChuyendi.dsDiem[j].thoigianxeden_luotdi = tgxd_luotdi;

                                                thoigianthembot2 -= thoigianthembot_Des;

                                                continue;
                                            }
                                        }

                                        TimeSpan tgxd_luotdi_truockhithem = copyOfChuyendi.dsDiem[j].thoigianxeden_luotdi;
                                        TimeSpan tgxd_luotdi_saukhithem = tgxd_luotdi_truockhithem.Add(tgxuatphatThaydoi1).Add(TimeSpan.FromMinutes(thoigianthembot1));
                                        copyOfChuyendi.dsDiem[j].thoigianxeden_luotdi = tgxd_luotdi_saukhithem;

                                        TimeSpan tgxd_luotve_truockhithem = copyOfChuyendi.dsDiem[j].thoigianxeden_luotve;
                                        TimeSpan tgxd_luotve_saukhithem = tgxd_luotve_truockhithem.Add(tgxuatphatThaydoi2).Add(TimeSpan.FromMinutes(thoigianthembot2));
                                        //copyOfChuyendi.dsDiem[j].thoigianxeden_luotve = gio_doubleToString(tgxd_luotve_saukhithem);
                                        int ind = j;
                                        foreach (point_detail pointdetail1 in copyOfChuyendi.dsDiem[j].dsdiemcungtoado)
                                        {
                                            if (pointIdIsDestination(pointdetail1))
                                            {
                                                //Console.WriteLine("lượt đi. iD: {0}, khung thời gian về: [{1}, {2}]", pointdetail1.id, pointdetail1.khungthoigianve[0], pointdetail1.khungthoigianve[1]);
                                                //tongthoigianduthuathem += tgxd_luotdi_truockhithem - tgxd_luotdi_saukhithem;     //
                                                //tongthoigianduthuathem += tgxd_luotve_saukhithem - tgxd_luotve_truockhithem;

                                                //check ddk lượt đi
                                                TimeSpan min = pointdetail1.khungthoigianden[0];
                                                TimeSpan max = pointdetail1.khungthoigianden[1];
                                                if (tgxd_luotdi_saukhithem < min || tgxd_luotdi_saukhithem > max)   //không thỏa
                                                {
                                                    dieukien1 = false;
                                                    breakRequest = true;
                                                    break;
                                                }
                                            }

                                            /////// lưu task điều kiện thời gian lượt về
                                            var task = new Task(() =>
                                            {
                                                if (ind != taskcapnhatgiove)
                                                {
                                                    //if (pointIdIsDestination(pointdetail1))
                                                    //{
                                                    //    thoigianxedoi += tgxd_luotve_saukhithem < gio_stringToDouble(pointdetail1.khungthoigianve[0]) ?
                                                    //        gio_stringToDouble(pointdetail1.khungthoigianve[0]) - tgxd_luotve_saukhithem : 0;
                                                    //}

                                                    //tgxd_luotve_saukhithem += thoigianxedoi;

                                                    tgxd_luotve_saukhithem -= tgdagiulai;

                                                    TimeSpan thoigianxedoi = (pointIdIsOrigrin(pointdetail1) || pointdetail1.khungthoigianve[0] <= tgxd_luotve_saukhithem) ? new TimeSpan(0) :
                                                        pointdetail1.khungthoigianve[0] - tgxd_luotve_saukhithem;

                                                    tgdagiulai += thoigianxedoi <= tgxpthaydoiconlai ? thoigianxedoi : tgxpthaydoiconlai;
                                                    tgxpthaydoiconlai -= thoigianxedoi <= tgxpthaydoiconlai ? thoigianxedoi : tgxpthaydoiconlai;

                                                    copyOfChuyendi.dsDiem[ind].thoigianxeden_luotve = tgxd_luotve_saukhithem;
                                                    taskcapnhatgiove = ind;
                                                }

                                                //Console.WriteLine("task {0} đag thực thi", ind);
                                                //// có 2 đk: tg đến của xe phải nhỏ hơn khungthoigianve[1], tg xe chờ phải nhỏ hơn tg chờ nhỏ nhất của những người trên xe
                                                //Console.WriteLine("lượt về. iD: {0}, khung thời gian về: [{1}, {2}]", pointdetail1.id, pointdetail1.khungthoigianve[0], pointdetail1.khungthoigianve[1]);

                                                if (pointIdIsDestination(pointdetail1))
                                                {
                                                    if (tgxd_luotve_saukhithem > pointdetail1.khungthoigianve[1] || pointdetail1.khungthoigianve[0] - tgxd_luotve_saukhithem > thoigianxecho)
                                                    {
                                                        dieukien2 = false;
                                                    }

                                                    //nếu điểm này thỏa thì cập nhật thoigianxecho
                                                    TimeSpan thoigianlechtoida = pointdetail1.khungthoigianve[1] - pointdetail1.khungthoigianve[0];
                                                    thoigianxecho = thoigianlechtoida < thoigianxecho ? thoigianlechtoida : thoigianxecho;
                                                }

                                            });
                                            TaskList.Add(task);
                                            // end task

                                        }
                                        if (breakRequest)
                                            break;
                                        if (j == vitriOrgTrongChuyenDi)
                                            thoigianthembot1 += thoigianthembot_Org;
                                        if (j == vitriDesTrongChuyenDi)
                                            thoigianthembot1 += thoigianthembot_Des;
                                    }
                                    //đến đây thì tất cả điểm đã thỏa mãn giờ thay đổi (tgxuatphatthaydoi) => thoát ra và điều kiện 1 thỏa
                                    breakRequest = true;
                                    break;  //chỉ xét 1 lần, thỏa hay không đều thoát
                                }
                            }
                        }
                        if (breakRequest)
                            break;
                    }
                    //}
                    if (!dieukien1)
                    {
                        continue;   //xét chuyến đi khác, nếu không có chuyến đi nào thỏa mãn thì nhảy đến (1)
                    }

                    //xét tiếp điều kiện 2
                    for (int i = TaskList.Count - 1; i >= 0; i--)
                    {
                        TaskList[i].RunSynchronously();
                        if (!dieukien2)
                            break;
                    }

                    if (!dieukien2)
                    {
                        continue;   //xét chuyến đi khác, nếu không có chuyến đi nào thỏa mãn thì nhảy đến (1)
                    }

                    for (int i = 0; i < sodiem; i++)
                    {
                        TimeSpan tgxd_luotdi = copyOfChuyendi.dsDiem[i].thoigianxeden_luotdi;
                        TimeSpan tgxd_luotve = copyOfChuyendi.dsDiem[i].thoigianxeden_luotve;

                        foreach (var pointdetail in copyOfChuyendi.dsDiem[i].dsdiemcungtoado)
                        {
                            if (pointIdIsOrigrin(pointdetail))
                            {
                                tongthoigian_sau += lichdangky[pointdetail.id].GioDen.Value - tgxd_luotdi;
                                tongthoigian_sau += tgxd_luotve - lichdangky[pointdetail.id].GioVe.Value;
                            }
                        }
                    }
                    double delta = (tongthoigian_sau - tongthoigian_truoc).TotalMinutes;
                    copyOfChuyendi.tongsonguoi += songuoidi;
                    //double tongthoigianlangphi = delta + tongthoigianduthuathem;
                    dsChuyenDiThoaDk.Add(new { vitrichuyendi = index, xedi = xehientai, chuyendimoi = copyOfChuyendi, thoigianlangphi = delta });
                }
                if (dschuyendi.Count > 0 && dsChuyenDiThoaDk.Count > 0)
                {
                    dsChuyenDiThoaDk.Sort((x, y) => x.thoigianlangphi.CompareTo(y.thoigianlangphi));

                    for (int i = 0; i < dsChuyenDiThoaDk.Count; i++)  //chọn trường hợp tốt nhất
                    {
                        int songuoihientai = dsChuyenDiThoaDk[i].xedi.songuoihientai;
                        if (songuoihientai + songuoidi > dsChuyenDiThoaDk[i].xedi.ThongTinXe.SoCho)
                        {
                            Xe xe = TimXe(songuoihientai + songuoidi);
                            if (xe == null)
                                continue;
                            dsChuyenDiThoaDk[i].xedi.trangthai = false;


                            dschuyendi[dsChuyenDiThoaDk[i].vitrichuyendi] = new ChuyenDi
                            {
                                dsDiem = new List<Point>(dsChuyenDiThoaDk[i].chuyendimoi.dsDiem),
                                tongsonguoi = dsChuyenDiThoaDk[i].chuyendimoi.tongsonguoi,
                                xedi = xe
                            };
                            themchuyendithanhcong = true;
                            break;
                        }
                        else
                        {
                            dschuyendi[dsChuyenDiThoaDk[i].vitrichuyendi] = new ChuyenDi
                            {
                                dsDiem = new List<Point>(dsChuyenDiThoaDk[i].chuyendimoi.dsDiem),
                                tongsonguoi = dsChuyenDiThoaDk[i].chuyendimoi.tongsonguoi,
                                xedi = dsChuyenDiThoaDk[i].chuyendimoi.xedi
                            };
                            dsChuyenDiThoaDk[i].xedi.songuoihientai += songuoidi;
                            themchuyendithanhcong = true;
                        }
                    }
                }
                if (!themchuyendithanhcong)     //tạo chuyến đi khác        (1)
                {
                    var xedi = TimXe(songuoidi);
                    if (xedi == null)
                        return;
                    dschuyendi.Add(new ChuyenDi
                    {
                        dsDiem = new List<Point>() { orgPoint, desPoint },
                        tongsonguoi = songuoidi,
                        xedi = xedi
                    });
                    double giodi = (double)(duration_in_traffic_matrix[OriginID][DestinationID]) / 60;
                    dschuyendi[dschuyendi.Count - 1].dsDiem[1].thoigianxeden_luotdi = desPoint.dsdiemcungtoado[0].khungthoigianden[1];
                    dschuyendi[dschuyendi.Count - 1].dsDiem[0].thoigianxeden_luotdi = desPoint.dsdiemcungtoado[0].khungthoigianden[1].Add(TimeSpan.FromMinutes(-giodi));

                    dschuyendi[dschuyendi.Count - 1].dsDiem[1].thoigianxeden_luotve = desPoint.dsdiemcungtoado[0].khungthoigianve[0];
                    dschuyendi[dschuyendi.Count - 1].dsDiem[0].thoigianxeden_luotve = desPoint.dsdiemcungtoado[0].khungthoigianve[0].Add(TimeSpan.FromMinutes(giodi));
                }

                for (int i = 0; i < dschuyendi[0].dsDiem.Count; i++)
                {
                    Console.WriteLine(dschuyendi[0].dsDiem[i].thoigianxeden_luotve);
                }
            }

            DateTime GetDateTime(string datetime)
            {
                string[] Y_M_D = { "", "", "" };
                int YYYY, MM, DD;

                char[] charArray = datetime.ToCharArray();
                var k = 0;
                for (var i = 0; i < charArray.Length; i++)
                {
                    if (k == 3)
                        break;

                    if (Char.IsDigit(charArray[i]))
                        Y_M_D[k] += charArray[i];
                    else
                        k++;
                }
                bool YYYYsuccess = Int32.TryParse(Y_M_D[0], out YYYY);
                bool MMsuccess = Int32.TryParse(Y_M_D[1], out MM);
                bool DDsuccess = Int32.TryParse(Y_M_D[2], out DD);
                if (!YYYYsuccess)
                    throw new Exception("Attempted conversion of " + Y_M_D[0] + " failed");
                if (!MMsuccess)
                    throw new Exception("Attempted conversion of " + Y_M_D[1] + " failed");
                if (!DDsuccess)
                    throw new Exception("Attempted conversion of " + Y_M_D[2] + " failed");

                return new DateTime(YYYY, MM, DD);
            }

            double gio_stringToDouble(TimeSpan gio)
            {
                var hh = gio.Hours;
                var mm = gio.Minutes;

                return hh + (double)mm / 60;
            }

            string gio_doubleToString(double gio)
            {
                int hh = (Int32)gio;
                int mm = (Int32)Math.Round((gio - hh) * 60);
                if (mm == 60)
                {
                    hh++;
                    mm = 0;
                }

                return hh.ToString().PadLeft(2, '0') + ":" + mm.ToString().PadLeft(2, '0');
            }

            TimeSpan[][] laykhungthoigian(TimeSpan gioden, TimeSpan giove, double thoigiandi)
            {
                double thoigiandi_phut = (double)thoigiandi / 60;
                int khoangchenhlech = ThoiGianLechToiDa(thoigiandi_phut);

                var khungthoigianden = new TimeSpan[] { gioden.Add(TimeSpan.FromMinutes(-khoangchenhlech)), gioden };
                var khungthoigianve  = new TimeSpan[] { giove, giove.Add(TimeSpan.FromMinutes(khoangchenhlech)) };
                return new TimeSpan[][] { khungthoigianden, khungthoigianve};
            }

            int ThoiGianLechToiDa(double thoigiandi)
            {
                int max = 240;  //4 giờ
                int min = 60;   //60 phút
                int t = (int)Math.Round(4 * Math.Sqrt(thoigiandi));
                return t < min ? min : (t > max ? max : t);
            }

            bool pointContaintAllOrigrin(Point point)
            {
                foreach (var item in point.dsdiemcungtoado)
                {
                    if (item.id >= lichdangky.Count)
                        return false;
                }
                return true;
            }

            bool pointContaintAllDestination(Point point)
            {
                if (point.dsdiemcungtoado[0].id >= lichdangky.Count)
                    return true;
                return false;
            }

            bool pointIdIsOrigrin(point_detail point)
            {
                return point.id < lichdangky.Count ? true : false;
            }

            bool pointIdIsDestination(point_detail point)
            {
                return point.id >= lichdangky.Count ? true : false;
            }

            int getOriginID(int DestinationID)
            {
                return DestinationID - lichdangky.Count;
            }

            int getDestinationID(int OriginID)
            {
                return OriginID + lichdangky.Count;
            }

            Xe TimXe(int songuoidi)
            {
                foreach (var xe in dsXe)
                {
                    if (xe.trangthai || xe.ThongTinXe.SoCho < songuoidi)
                        continue;
                    xe.trangthai = true;
                    xe.songuoihientai = songuoidi;
                    return xe.ThongTinXe;
                }
                return null;
            }
        }

        //here
        public static dynamic get_ORG_DES_WAY(List<Point> list)
        {
            Coordinate origin = list[0].location, destination = list[list.Count - 1].location;
            List<Coordinate> waypoints = new List<Coordinate>();
            for (int i = 1; i < list.Count - 1; i++)
            {
                waypoints.Add(list[i].location);
            }
            return new { origin, destination, waypoints };
        }
    }
}