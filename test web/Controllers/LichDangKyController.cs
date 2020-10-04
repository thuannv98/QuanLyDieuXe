using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using test_web.Models;

namespace test_web.Controllers
{
    public class LichDangKyController : Controller
    {
        dieuxe1Entities db = new dieuxe1Entities();
        // GET: LichDangKy
        public ActionResult Index()
        {
            //var dsdangky = from dkl in db.tblDangKyliches
            //               join chitiet in db.tblDangKyLichChiTiets  on dkl.DangKyLichId equals chitiet.DangKyLichId
            //               where chitiet.NgayDen == DateTime.Now.AddDays(1).Date

            var dsdangky = db.tblDangKyLichChiTiets.ToList();
            ViewBag.dsdangky = dsdangky;
            return View();
        }

        public ActionResult DsLichNgayMai()
        {
            var ngaymai = DateTime.Now.AddDays(1).Date;
            //var dslichngaymai = (from dkl in db.tblDangKyliches
            //                    join chitiet in db.tblDangKyLichChiTiets on dkl.DangKyLichId equals chitiet.DangKyLichId
            //                    join lienhe in db.tblLienHes on dkl.NguoiTao equals lienhe.LienHeID
            //                    where chitiet.NgayDen == ngaymai
            //                    select new LichDangKy { NguoiDangKy = lienhe.TenLienHe, DangKyLichChiTiet = chitiet }).ToList();

            var dslichngaymai = new List<LichDangKy>()
            {
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.845966, 106.765224", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "30-38 Dân Chủ, Bình Thọ, Thủ Đức", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), NgayDen = new DateTime(2020, 10, 05, 00, 00, 00), TenNguoiDi="Quang", DangKyLichId = 1, DangKyLichChiTietId=1} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.838085, 106.634272", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "17 / 6A Phan Huy Ích, Phường 15, Gò Vấp", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(15, 00, 00), TenNguoiDi="Cường, Tuấn", DangKyLichId = 1, DangKyLichChiTietId=2} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.769082, 106.702199", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "16 Nam Kỳ Khởi Nghĩa, Phường Nguyễn Thái Bình, Quận 1", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(15, 00, 00), TenNguoiDi="Bình, Chị Hảo" , DangKyLichId = 1, DangKyLichChiTietId=3} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.769082, 106.702199", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "16 Nam Kỳ Khởi Nghĩa, Phường Nguyễn Thái Bình, Quận 1", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi="Phú, Quốc", DangKyLichId = 1, DangKyLichChiTietId=4} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.868034, 106.780647", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "940 Xa lộ Đại Hàn, Phường Linh Trung, Thủ Đức", GioDen = new TimeSpan(09, 00, 00), GioVe = new TimeSpan(15, 00, 00), TenNguoiDi="Hợp, Quân, Bích", DangKyLichId = 1, DangKyLichChiTietId=5} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.838781, 106.672548", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "236a Nguyễn Văn Lượng, Phường 6, Gò Vấp", GioDen = new TimeSpan(09, 00, 00),  GioVe = new TimeSpan(17, 00, 00), TenNguoiDi="Thuận", DangKyLichId = 1, DangKyLichChiTietId=6} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.769082, 106.702199", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "16 Nam Kỳ Khởi Nghĩa, Phường Nguyễn Thái Bình, Quận 1", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(15, 00, 00), TenNguoiDi="Thiệu, Thức", DangKyLichId = 1, DangKyLichChiTietId=7} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.835409, 106.635244", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "243 Phan Huy Ích, Phường 12, Gò Vấp", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(15, 00, 00), TenNguoiDi="Bình, Nhưỡng", DangKyLichId = 1, DangKyLichChiTietId=8} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.884010, 106.586954", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "47 Xuyên Á, Xuân Thới Sơn, Hóc Môn", GioDen = new TimeSpan(08, 00, 00),  GioVe = new TimeSpan(16, 00, 00), TenNguoiDi="Phú, Linh, Như", DangKyLichId = 1, DangKyLichChiTietId=9} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 1, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.787637, 106.686105", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "209 - 201 Nam Kỳ Khởi Nghĩa, Phường 7, Quận 3", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi="Chị Tâm", DangKyLichId = 1, DangKyLichChiTietId=10 } },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.832282, 106.778043", ToaDoDen = "10.803820, 106.689993", NoiDi = "48 Đường Tăng Nhơn Phú, Tăng Nhơn Phú B, Quận 9", NoiDen = "24a Phan Đăng Lưu, Phường 6, Bình Thạnh", GioDen = new TimeSpan(09, 00, 00),  GioVe = new TimeSpan(16, 00, 00), TenNguoiDi="Phương", DangKyLichId = 1, DangKyLichChiTietId=11} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.803109, 106.737720", ToaDoDen = "10.842808, 106.615312", NoiDi = "70 Nguyễn Duy Hiệu, Thảo Điền, Quận 2", NoiDen = "An Sương 84, Xa lộ Đại Hàn, Đông Hưng Thuận, Hóc Môn", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi = "Quang" , DangKyLichId = 1, DangKyLichChiTietId=12} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.886754, 106.759661", ToaDoDen = "10.836570, 106.654286", NoiDi = "338-350 Nguyễn Tri Phương, An Bình, Dĩ An, Bình Dương", NoiDen = "693 Quang Trung, Phường 8, Gò Vấp", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi = "Quang" , DangKyLichId = 1, DangKyLichChiTietId=13} },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.774183, 106.722131", ToaDoDen = "10.813318, 106.578647", NoiDi = "232 Đường Mai Chí Thọ, An Lợi Đông, Quận 2", NoiDen = "2300 Đường Vĩnh Lộc, Vĩnh Lộc B, Bình Chánh", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi = "Quang", DangKyLichId = 1, DangKyLichChiTietId=14 } },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.848119, 106.718481", ToaDoDen = "10.806229, 106.627437", NoiDi = "1/109 QL13, Hiệp Bình Phước, Thủ Đức", NoiDen = "34 Lê Trọng Tấn, Sơn Ký, Tân Phú", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi = "Quang", DangKyLichId = 1, DangKyLichChiTietId=15 } },
                new LichDangKy { DangKyLichChiTiet = new tblDangKyLichChiTiet { SoNguoi = 2, ToaDoDi = "10.795682, 106.675471", ToaDoDen = "10.832169, 106.621990", NoiDi = "163 Nguyễn Văn Trỗi, Phường 11, Phú Nhuận", NoiDen = "Chùa Vĩnh Phước, Trường Chinh, Vinh Phuoc Pagoda, Quận 12", GioDen = new TimeSpan(08, 00, 00), GioVe = new TimeSpan(16, 00, 00), TenNguoiDi="Quang", DangKyLichId = 1, DangKyLichChiTietId=16 } }
            };

            //ViewBag.dslichngaymai = dslichngaymai;
            return View(dslichngaymai);
        }
    }
}