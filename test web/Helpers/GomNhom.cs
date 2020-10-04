using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
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

        public Point()
        {

        }
        public Point(Point p)
        {
            location = new Coordinate(p.location);
            diachi = p.diachi;
            distance = p.distance;
            thoigianxeden_luotdi = p.thoigianxeden_luotdi;
            thoigianxeden_luotve = p.thoigianxeden_luotve;
            dsdiemcungtoado = new List<point_detail>(p.dsdiemcungtoado.Count);
            foreach (var pd in p.dsdiemcungtoado)
            {
                dsdiemcungtoado.Add(new point_detail(pd));
            }
        }
    }
    public class point_detail
    {
        public int id { get; set; }
        public int songuoi { get; set; }
        public string tennguoidi { get; set; }
        public TimeSpan thoigianden { get; set; }
        public TimeSpan thoigianve { get; set; }
        public TimeSpan[] khungthoigianden { get; set; }
        public TimeSpan[] khungthoigianve { get; set; }
        public int DangKyLichChiTietId { get; set; }
        public int DangKyLichId { get; set; }
        public string NoiDi { get; set; }
        public string NoiDen { get; set; }

        public point_detail()
        {

        }
        public point_detail(point_detail pd)
        {
            if (pd != null)
            {
                id = pd.id;
                songuoi = pd.songuoi;
                tennguoidi = pd.tennguoidi;
                thoigianden = pd.thoigianden;
                thoigianve = pd.thoigianve;
                if (pd.khungthoigianden != null)
                {
                    khungthoigianden = new TimeSpan[pd.khungthoigianden.Length];
                    pd.khungthoigianden.CopyTo(khungthoigianden, 0);
                    khungthoigianve = new TimeSpan[pd.khungthoigianve.Length];
                    pd.khungthoigianve.CopyTo(khungthoigianve, 0);
                }
                DangKyLichChiTietId = pd.DangKyLichChiTietId;
                DangKyLichId = pd.DangKyLichId;
                NoiDi = pd.NoiDi;
                NoiDen = pd.NoiDen;
            }
        }
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
        TimeSpan Thoigianxechotoida = TimeSpan.FromHours(2);    // khi đón nhân viên tại nơi công tác (lượt về), nếu xe đến sớm thì thời gian tối đa xe chờ là bao nhiêu (giờ)?

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
        //void MiddleCall()
        //{
        //    ThucHien();
        //}
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

                                bool isExist = false;
                                foreach (var point in chuyendi)
                                {
                                    if (newlocation.IsSame(point.location))
                                    {
                                        isExist = true;
                                        point.dsdiemcungtoado.Add(new point_detail
                                        {
                                            id = getDestinationID(IdPoint)
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
                                            id = getDestinationID(IdPoint)
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

                                int sodiem = chuyen.Count;
                                if (sodiem > 2)
                                {
                                    int id1 = chuyen[sodiem - 3].dsdiemcungtoado.First().id;
                                    int id2 = chuyen[sodiem - 2].dsdiemcungtoado.First().id;
                                    int id3 = chuyen[sodiem - 1].dsdiemcungtoado.First().id;

                                    double dist1 = distancematrix[id1][id2] + distancematrix[id2][id3];
                                    double dist2 = distancematrix[id1][id3] + distancematrix[id3][id2];

                                    if (dist1 > dist2)
                                    {
                                        //int length = chuyen.Count;
                                        var temp = chuyen[sodiem - 2];
                                        chuyen[sodiem - 2] = chuyen[sodiem - 1];
                                        chuyen[sodiem - 1] = temp;
                                    }
                                }

                                dsChuyenDiTheoDiaDiem.Add(chuyen);
                            }
                        }
                    }
                }

                //phân theo thời gian
                //Parallel.ForEach(dsChuyenDiTheoDiaDiem, locthoigian);
                locthoigian(dsChuyenDiTheoDiaDiem[0]);
                locthoigian(dsChuyenDiTheoDiaDiem[1]);

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

            //foreach (var chuyenDi in DsChuyenDi)
            //{
            //    foreach (var d in chuyenDi.dsDiem)
            //    {
            //        foreach (var item in d.dsdiemcungtoado)
            //        {
            //            if (pointIdIsDestination(item))
            //            {
            //                total += Math.Abs(gio_stringToDouble(item.thoigianden) - gio_stringToDouble(d.thoigianxeden_luotdi))
            //                    + Math.Abs(gio_stringToDouble(item.thoigianve) - gio_stringToDouble(d.thoigianxeden_luotve));
            //            }
            //        }

            //    }
            //}
            //double tgchotrungbinh = total / lichdangky.Count;
            //Console.WriteLine("Thời gian chờ trung bình: {0} (h)", tgchotrungbinh);

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
                string tennguoidi = lichdangky[OriginID].TenNguoiDi;
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
                        tennguoidi = tennguoidi,
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
                        tennguoidi = tennguoidi,
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
                    Console.WriteLine("chuyến: " + index);
                    //if (index == 3)
                    //{
                    //    var a = "DSAS";
                    //}
                    //if (DestinationID == 17 && index == 1)
                    //    break;
                    //if (DestinationID == 23 && index == 0)
                    //{ var a = 0; }
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
                        dsDiem = new List<Point>(chuyendi.dsDiem.Count),
                        tongsonguoi = chuyendi.tongsonguoi,
                        xedi = chuyendi.xedi
                    };
                    chuyendi.dsDiem.ForEach((item) =>
                    {
                        copyOfChuyendi.dsDiem.Add(new Point(item));
                    });

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

                    bool DaCoToaDoOrigin = false;
                    bool DaCoToaDoDestination = false;

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
                                        //if (Index == copyOfChuyendi.dsDiem.Count - 1)
                                        //    DesCuoiDanhSach = true;
                                        DestinationAdded = true;
                                    }
                                    else
                                    {   //chèn điểm vào trước vị trí Index
                                        if (Index == copyOfChuyendi.dsDiem.Count)
                                        {
                                            copyOfChuyendi.dsDiem.Add(desPoint);
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
                        vitriOrgTrongChuyenDi = themdiemvaods(orgPoint, copyOfChuyendi.dsDiem);
                        DaCoToaDoOrigin = copyOfChuyendi.dsDiem[vitriOrgTrongChuyenDi].dsdiemcungtoado.Count > 1;

                        vitriDesTrongChuyenDi = themdiemvaods(desPoint, copyOfChuyendi.dsDiem);
                        DaCoToaDoDestination = copyOfChuyendi.dsDiem[vitriDesTrongChuyenDi].dsdiemcungtoado.Count > 1;

                        if (vitriDesTrongChuyenDi <= vitriOrgTrongChuyenDi)
                        {
                            Console.WriteLine("đi ngc chiều.");
                            continue;
                        }
                    }


                    int themdiemvaods(Point point, List<Point> dsDiem)
                    {
                        int id = point.dsdiemcungtoado.FirstOrDefault().id;
                        double? min = null;
                        int ind = 0;
                        for (int i = 0; i < dsDiem.Count; i++)
                        {
                            if (i == 0 && point.location.IsSame(dsDiem[i].location))
                            {
                                dsDiem[i].dsdiemcungtoado.Add(point.dsdiemcungtoado.FirstOrDefault());
                                return i;
                            }
                            if (i != dsDiem.Count - 1 && point.location.IsSame(dsDiem[i + 1].location))
                            {
                                dsDiem[i + 1].dsdiemcungtoado.Add(point.dsdiemcungtoado.FirstOrDefault());
                                return i + 1;
                            }
                            int id1 = dsDiem[i].dsdiemcungtoado.FirstOrDefault().id;
                            double fee = distancematrix[id1][id];
                            if (i != dsDiem.Count - 1)
                            {
                                int id2 = dsDiem[i + 1].dsdiemcungtoado.FirstOrDefault().id;
                                fee += distancematrix[id][id2] - distancematrix[id1][id2];
                            }
                            if (!min.HasValue)
                                min = fee;
                            if (fee < min)
                            {
                                min = fee;
                                ind = i + 1;
                            }
                        }
                        if (ind == dsDiem.Count)
                            dsDiem.Add(point);
                        else
                            dsDiem.Insert(ind, point);

                        return ind;
                    }

                    int sodiem = copyOfChuyendi.dsDiem.Count;

                    bool chapnhanthem = true;

                    CancellationTokenSource cts = new CancellationTokenSource();
                    CancellationToken ct = cts.Token;
                    ct.Register(() =>
                    {
                        Console.WriteLine("Token is canceled");
                    });

                    Task luotdi = Task.Run(() =>
                    {
                        TimeSpan tgxuatphatThaydoi1 = new TimeSpan(0);
                        int solankiemtra = 0;
                        bool breakwhileloop = false;
                        while (!breakwhileloop)
                        {
                            solankiemtra++;
                            bool thoayeucau = true;
                            for (int j = 0; j < sodiem; j++)
                            {
                                if (ct.IsCancellationRequested)
                                    return;
                                TimeSpan gioxeden = new TimeSpan(0);
                                if (j == 0)
                                {
                                    if (j == vitriOrgTrongChuyenDi && !DaCoToaDoOrigin && solankiemtra == 1)
                                    {
                                        var id0 = copyOfChuyendi.dsDiem[0].dsdiemcungtoado[0].id;
                                        var id1 = copyOfChuyendi.dsDiem[1].dsdiemcungtoado[0].id;
                                        var id2 = copyOfChuyendi.dsDiem[2].dsdiemcungtoado[0].id;

                                        if (j + 1 == vitriDesTrongChuyenDi && !DaCoToaDoDestination)
                                        {
                                            gioxeden = copyOfChuyendi.dsDiem[j + 2].thoigianxeden_luotdi;
                                            gioxeden = gioxeden.Add(TimeSpan.FromMinutes(-((double)duration_in_traffic_matrix[id0][id1] + (double)duration_in_traffic_matrix[id1][id2]) / 60 ));
                                        }
                                        else
                                        {
                                            gioxeden = copyOfChuyendi.dsDiem[j + 1].thoigianxeden_luotdi;
                                            gioxeden = gioxeden.Add(TimeSpan.FromMinutes(-(double)duration_in_traffic_matrix[id0][id1] / 60));
                                        }
                                    }
                                    else
                                    {
                                        gioxeden = copyOfChuyendi.dsDiem[0].thoigianxeden_luotdi;
                                    }
                                    if (solankiemtra == 2)
                                        gioxeden.Add(tgxuatphatThaydoi1);
                                }
                                else
                                {
                                    var id_truoc = copyOfChuyendi.dsDiem[j - 1].dsdiemcungtoado[0].id;
                                    var id = copyOfChuyendi.dsDiem[j].dsdiemcungtoado[0].id;
                                    gioxeden = copyOfChuyendi.dsDiem[j - 1].thoigianxeden_luotdi;
                                    gioxeden = gioxeden.Add(TimeSpan.FromMinutes((double)duration_in_traffic_matrix[id_truoc][id] / 60));
                                }
                                //gioxeden = Math.Round(gioxeden, 1);
                                copyOfChuyendi.dsDiem[j].thoigianxeden_luotdi = gioxeden;
                                foreach (var pointdetail1 in copyOfChuyendi.dsDiem[j].dsdiemcungtoado)
                                {
                                    if (pointIdIsDestination(pointdetail1))
                                    {
                                        var min = pointdetail1.khungthoigianden[0];
                                        var max = pointdetail1.khungthoigianden[1];

                                        if ((gioxeden < min || gioxeden > max) && solankiemtra == 2)
                                        {
                                            chapnhanthem = false;
                                            cts.Cancel();
                                            break;
                                        }

                                        if (gioxeden < min && tgxuatphatThaydoi1 < min - gioxeden)  // tìm thời gian sớm hơn lớn nhất
                                        {
                                            thoayeucau = false;
                                            tgxuatphatThaydoi1 = min - gioxeden;
                                        }
                                        if (gioxeden > max && tgxuatphatThaydoi1 > max - gioxeden)  // tìm thời gian lùi lớn nhất
                                        {
                                            thoayeucau = false;
                                            tgxuatphatThaydoi1 = max - gioxeden;
                                        }
                                    }
                                }
                                if (!chapnhanthem)
                                    break;
                            }
                            if (thoayeucau || !chapnhanthem)
                                breakwhileloop = true;
                        }
                    });

                    Task luotve = Task.Run(() =>
                    {
                        TimeSpan tgxuatphatThaydoi = new TimeSpan(0);
                        int solankiemtra = 0;
                        bool breakwhileloop = false;
                        while (!breakwhileloop)
                        {
                            solankiemtra++;
                            bool thoayeucau = true;
                            TimeSpan thoigianxecho = new TimeSpan(0);
                            //double xecho = 0;

                            for (int j = sodiem - 1; j >= 0; j--)
                            {
                                if (ct.IsCancellationRequested)
                                    return;
                                TimeSpan gioxeden = new TimeSpan(0);
                                if (j == sodiem - 1)
                                {
                                    if (j == vitriDesTrongChuyenDi && !DaCoToaDoDestination && solankiemtra == 1)
                                    {
                                        var id0 = copyOfChuyendi.dsDiem[sodiem - 1].dsdiemcungtoado[0].id;
                                        var id1 = copyOfChuyendi.dsDiem[sodiem - 2].dsdiemcungtoado[0].id;
                                        var id2 = copyOfChuyendi.dsDiem[sodiem - 3].dsdiemcungtoado[0].id;

                                        if (j - 1 == vitriOrgTrongChuyenDi && !DaCoToaDoOrigin)
                                        {
                                            gioxeden = copyOfChuyendi.dsDiem[j - 2].thoigianxeden_luotve;
                                            gioxeden = gioxeden.Add(TimeSpan.FromMinutes(-((double)duration_in_traffic_matrix[id0][id1] + (double)duration_in_traffic_matrix[id1][id2]) / 60));
                                        }
                                        else
                                        {
                                            gioxeden = copyOfChuyendi.dsDiem[j - 1].thoigianxeden_luotve;
                                            gioxeden = gioxeden.Add(TimeSpan.FromMinutes(-(double)duration_in_traffic_matrix[id0][id1] / 60));
                                        }
                                    }
                                    else
                                    {
                                        gioxeden = copyOfChuyendi.dsDiem[sodiem - 1].thoigianxeden_luotve;
                                    }
                                    if (solankiemtra == 2)
                                        gioxeden += tgxuatphatThaydoi;
                                }
                                else
                                {
                                    var id_truoc = copyOfChuyendi.dsDiem[j + 1].dsdiemcungtoado[0].id;
                                    var id = copyOfChuyendi.dsDiem[j].dsdiemcungtoado[0].id;
                                    gioxeden = copyOfChuyendi.dsDiem[j + 1].thoigianxeden_luotve;
                                    gioxeden = gioxeden.Add(thoigianxecho);
                                    gioxeden = gioxeden.Add(TimeSpan.FromMinutes((double)duration_in_traffic_matrix[id_truoc][id] / 60));

                                    thoigianxecho = new TimeSpan(0);
                                }
                                //gioxeden = Math.Round(gioxeden, 1);
                                foreach (var pointdetail1 in copyOfChuyendi.dsDiem[j].dsdiemcungtoado)
                                {
                                    if (pointIdIsDestination(pointdetail1))
                                    {
                                        var min = pointdetail1.khungthoigianve[0];
                                        var max = pointdetail1.khungthoigianve[1];

                                        if ((min - gioxeden > Thoigianxechotoida || gioxeden > max) && solankiemtra == 2)
                                        {
                                            chapnhanthem = false;
                                            cts.Cancel();
                                            break;
                                        }

                                        if (gioxeden < min)
                                        {
                                            thoigianxecho = min - gioxeden > thoigianxecho ? min - gioxeden : thoigianxecho;
                                            //if (j == sodiem - 1)
                                            //    xecho = thoigianxecho;
                                            if (thoigianxecho > Thoigianxechotoida && j != sodiem - 1)  // thời gian chờ lớn nhất
                                            {
                                                thoayeucau = false;
                                                tgxuatphatThaydoi = thoigianxecho - Thoigianxechotoida;
                                            }
                                        }
                                        else if (gioxeden > max && tgxuatphatThaydoi > max - gioxeden)  // tìm thời gian lùi lớn nhất
                                        {
                                            thoayeucau = false;
                                            tgxuatphatThaydoi = max - gioxeden;
                                        }

                                    }
                                }
                                if (!chapnhanthem)
                                    break;
                                copyOfChuyendi.dsDiem[j].thoigianxeden_luotve = gioxeden;
                            }
                            if (thoayeucau || !chapnhanthem)
                                breakwhileloop = true;

                            //k quan trọng
                            //if (solankiemtra == 2 && chapnhanthem)
                            //    copyOfChuyendi.dsDiem.Last().thoigianxeden_luotve = gio_doubleToString(Math.Round(gio_stringToDouble(copyOfChuyendi.dsDiem.Last().thoigianxeden_luotve) + xecho, 1)) ;
                        }
                    });

                    List<Task> Tasks = new List<Task> { luotdi, luotve };
                    //Task.WaitAll(Tasks.ToArray());
                    Tasks[0].Wait();
                    Tasks[1].Wait();

                    if (!chapnhanthem)
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
                    dsChuyenDiThoaDk.Add(new
                    {
                        vitrichuyendi = index,
                        xedi = xehientai,
                        chuyendimoi = new ChuyenDi
                        {
                            dsDiem = new List<Point>(copyOfChuyendi.dsDiem.Count),
                            tongsonguoi = copyOfChuyendi.tongsonguoi,
                            xedi = copyOfChuyendi.xedi
                        },
                        thoigianlangphi = delta
                    });

                    copyOfChuyendi.dsDiem.ForEach((item) =>
                    {
                        dsChuyenDiThoaDk.Last().chuyendimoi.dsDiem.Add(new Point(item));
                    });
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
                                dsDiem = new List<Point>(dsChuyenDiThoaDk[i].chuyendimoi.dsDiem.Count),
                                tongsonguoi = dsChuyenDiThoaDk[i].chuyendimoi.tongsonguoi,
                                xedi = dsChuyenDiThoaDk[i].chuyendimoi.xedi
                            };
                            dsChuyenDiThoaDk[i].chuyendimoi.dsDiem.ForEach((item) =>
                            {
                                dschuyendi[dsChuyenDiThoaDk[i].vitrichuyendi].dsDiem.Add(new Point(item));
                            });
                            dsChuyenDiThoaDk[i].xedi.songuoihientai += songuoidi;
                            themchuyendithanhcong = true;
                            break;
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
                    double giodi = (double)(duration_in_traffic_matrix[OriginID][DestinationID]) / 60;   // tổng phút
                    dschuyendi[dschuyendi.Count - 1].dsDiem[1].thoigianxeden_luotdi = desPoint.dsdiemcungtoado[0].khungthoigianden[1];
                    dschuyendi[dschuyendi.Count - 1].dsDiem[0].thoigianxeden_luotdi = desPoint.dsdiemcungtoado[0].khungthoigianden[1].Add(TimeSpan.FromMinutes(-giodi));

                    dschuyendi[dschuyendi.Count - 1].dsDiem[1].thoigianxeden_luotve = desPoint.dsdiemcungtoado[0].khungthoigianve[0];
                    dschuyendi[dschuyendi.Count - 1].dsDiem[0].thoigianxeden_luotve = desPoint.dsdiemcungtoado[0].khungthoigianve[0].Add(TimeSpan.FromMinutes(giodi));
                }
            }

            //double gio_stringToDouble(TimeSpan gio)
            //{
            //    var hh = gio.Hours;
            //    var mm = gio.Minutes;

            //    return hh + (double)mm / 60;
            //}

            //string gio_doubleToString(double gio)
            //{
            //    int hh = (Int32)gio;
            //    int mm = (Int32)Math.Round((gio - hh) * 60);
            //    if (mm == 60)
            //    {
            //        hh++;
            //        mm = 0;
            //    }

            //    return hh.ToString().PadLeft(2, '0') + ":" + mm.ToString().PadLeft(2, '0');
            //}

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