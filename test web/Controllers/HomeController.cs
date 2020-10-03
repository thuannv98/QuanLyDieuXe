using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Design;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using test_web.Models;
namespace test_web.Controllers
{
    public class HomeController : Controller
    {
        private dieuxe1Entities db = new dieuxe1Entities();
        //private string constr = ConfigurationManager.ConnectionStrings["dieuxeEntities"].ToString();
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult DieuxeModel()
        {
            //SqlConnection _con = new SqlConnection(constr);
            //SqlDataAdapter _da = new SqlDataAdapter("select * from tblLienHe where LoailienHe=1", constr);
            //DataTable _dt = new DataTable();
            //SqlDataAdapter _dd = new SqlDataAdapter("select * from tblDangKyLichChiTiet", constr);
            //DataTable dsdiemdi = new DataTable();
            //_da.Fill(_dt);
            //_dd.Fill(dsdiemdi);
            List<tblLienHe> lh = db.tblLienHes.Where(i => i.LoailienHe == 1).ToList();
            List<tblDangKyLichChiTiet> ct = db.tblDangKyLichChiTiets.ToList();
            ViewBag.DriverList = SelectList(lh);
            ViewBag.DiemDungList = SelectList(ct);
            return View();
        }
        [HttpPost]
        public ActionResult DieuxeModel(DieuxeModel _member)
        {
            return View();
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult listDieuxe()
        {
            var dxmodel = db.tblDieuXes.ToList();
            var listTaixe = db.tblLienHes.Where(i => i.LoailienHe == 1).ToList();


            //Example 1 Using ViewBag
            ViewBag.dxmodel1 = dxmodel;
            ViewBag.listTaiXe = SelectList(listTaixe);


            //Example 2 Using ViewData
            ViewData["dxmodel2"] = dxmodel;

            //Example 3 Using TempData
            TempData["dxmodel3"] = dxmodel;

            return View();
        }
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        [NonAction]
        public SelectList SelectList(List<tblLienHe> lh)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            foreach (var item in lh)
            {
                list.Add(new SelectListItem()
                {
                    Text = item.TenLienHe.ToString(),
                    Value = item.LienHeID.ToString()
                });
            }

            return new SelectList(list, "Value", "Text");
        }

        [NonAction]
        public SelectList SelectList(List<tblDangKyLichChiTiet> lh)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            foreach (var item in lh)
            {
                list.Add(new SelectListItem()
                {
                    Text = item.NoiDi.ToString(),
                    Value = item.DangKyLichChiTietId.ToString()
                });
            }

            return new SelectList(list, "Value", "Text");
        }


        [HttpPost]
        public ActionResult listDieuxe(tblDieuXe dx)
        {
            return View();
        }
        [NonAction]
        public SelectList ToSelectList(DataTable table, string valueField, string textField)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            foreach (DataRow row in table.Rows)
            {
                list.Add(new SelectListItem()
                {
                    Text = row[textField].ToString(),
                    Value = row[valueField].ToString()
                });
            }

            return new SelectList(list, "Value", "Text");
        }
    }
}