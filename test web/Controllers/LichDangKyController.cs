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
            var dslichngaymai = (from dkl in db.tblDangKyliches
                                join chitiet in db.tblDangKyLichChiTiets on dkl.DangKyLichId equals chitiet.DangKyLichId
                                join lienhe in db.tblLienHes on dkl.NguoiTao equals lienhe.LienHeID
                                where chitiet.NgayDen == ngaymai
                                select new LichDangKy { NguoiDangKy = lienhe.TenLienHe, DangKyLichChiTiet = chitiet }).ToList();

            //ViewBag.dslichngaymai = dslichngaymai;
            return View(dslichngaymai);
        }
    }
}