using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace test_web.Models
{
    public class Chitietnguoidi
    {
        public int DangKyLichChiTietId { get; set; }
        public int DangKyLichId { get; set; }
        public string NoiDi { get; set; }
        public string NoiDen { get; set; }
        public TimeSpan GioDi { get; set; }
    }

    public class LichDieuXe
    {
        public int MaDieuxe { get; set; }
        public string NoiDi { get; set; }
        public string NoiDen { get; set; }
        public TimeSpan GioDi { get; set; }
        public TimeSpan GioVe { get; set; }
        public int TrangThai { get; set; }      //string -> int
        public string TuyenDuongDi { get; set; }        //modify
        public string TuyenDuongVe { get; set; }        //add
        public string GhiChu { get; set; }
        public string BienKiemSoat { get; set; }
        public decimal ChiPhi { get; set; }
        public DateTime NgayDangKy { get; set; }
        public int NguoiTao { get; set; }
        public DateTime NgayCapNhat { get; set; }
        public int NguoiCapnhat { get; set; }
        public DateTime NgayDi { get; set; }
        public int SoNguoi { get; set; }
        public string DsDiemDung { get; set; }
        public List<Chitietnguoidi> listChitietnguoidi { get; set; }
        public int TaiXe { get; set; }
    }
}
