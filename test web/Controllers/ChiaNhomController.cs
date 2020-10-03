using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using test_web.Helpers;
using test_web.Models;

namespace test_web.Controllers
{
    public class ChiaNhomController : Controller
    {
        dieuxe1Entities db = new dieuxe1Entities();
        // GET: ChiaNhom
        //public ActionResult Index()
        //{

        //    return View();
        //}
        //List<tblDangKyLichChiTiet> data = new List<tblDangKyLichChiTiet>();
        public ActionResult Index(List<LichDangKy> dslichngaymai)
        {
            ViewBag.dslichngaymai = dslichngaymai;
            List<tblDangKyLichChiTiet> data = new List<tblDangKyLichChiTiet>();
            foreach (var lich in dslichngaymai)
            {
                data.Add(lich.DangKyLichChiTiet);
            }

            return View(data);
        }

        public async Task<ActionResult> PhanTich(List<tblDangKyLichChiTiet> data)
        {
            if (data == null || data.Count == 0)
                return View("Index");

            var ketqua = new GomNhom(data);
            await ketqua.ThucHien();

            var dsdieuxe = ketqua.DsDieuXe;
            var dschuyendi = ketqua.DsChuyenDi;

            string j1 = ketqua.json1, j2 = ketqua.json2;

            Debug.WriteLine("j1: \n" + j1);
            Debug.WriteLine("j2: \n" + j2);

            ViewBag.dschuyendi = dschuyendi;
            ViewBag.dsdieuxe = dsdieuxe;
            return View("Index");
        }
    }
}