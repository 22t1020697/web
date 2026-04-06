using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Partner;
using SV22T1020697.Admin;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020697.Admin.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến người giao hàng (Shipper)
    /// Bao gồm: hiển thị danh sách, tìm kiếm, thêm mới, cập nhật và xóa dữ liệu
    /// </summary>

    [Authorize]
    public class ShipperController : Controller
    {
        //private const int PAGESIZE = 10;
        private const string SHIPPER_SEARCH_INPUT = "ShipperSearchInput";

        /// <summary>
        /// Hiển thị danh sách shipper
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH_INPUT);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        /// <summary>
        /// Thực hiện tìm kiếm và trả về kết quả danh sách shipper
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Danh sách shipper phù hợp</returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListShippersAsync(input);

            ApplicationContext.SetSessionData(SHIPPER_SEARCH_INPUT, input);

            return View(result); // hoặc PartialView nếu dùng AJAX
        }

        /// <summary>
        /// Hiển thị form tạo mới shipper
        /// </summary>
        /// <returns>View nhập thông tin shipper</returns>
        public IActionResult Create()
        {
            ViewData["Title"] = "Bổ sung shipper";
            ViewBag.AllowDelete = false;

            return View("Edit", new Shipper() { ShipperID = 0 });
        }


        /// <summary>
        /// Hiển thị biểu mẫu cập nhật shipper
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật người giao hàng";

            var model = await PartnerDataService.GetShipperAsync(id);

            if (model == null)
                return RedirectToAction("Index");

            //kiểm tra có được phép xóa hay ko 
            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu shipper (Insert + Update)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewData["Title"] = data.ShipperID == 0
                ? "Bổ sung shipper"
                : "Cập nhật shipper";

            // ===== VALIDATE =====
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

            // Chuẩn hóa dữ liệu
            data.Phone ??= "";

            // Nếu có lỗi thì quay lại form
            if (!ModelState.IsValid)
            {
                ViewBag.AllowDelete = false;
                return View("Edit", data);
            }

            // ===== SAVE =====
            if (data.ShipperID == 0)
                await PartnerDataService.AddShipperAsync(data);
            else
                await PartnerDataService.UpdateShipperAsync(data);

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa shipper
        /// </summary>
        /// <param name="id">Mã shipper cần xóa</param>
        /// <returns>View xác nhận xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra ràng buộc dữ liệu
            ViewBag.AllowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));

            return View(model);
        }

        /// <summary>
        /// Thực hiện xóa shipper khỏi hệ thống
        /// </summary>
        /// <param name="id">Mã shipper cần xóa</param>
        /// <param name="confirm">Biến xác nhận (không sử dụng)</param>
        /// <returns>Chuyển về trang danh sách</returns>
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string confirm = "")
        {
            await PartnerDataService.DeleteShipperAsync(id);
            return RedirectToAction("Index");
        }
    }
}