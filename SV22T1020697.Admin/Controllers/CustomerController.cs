using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020697.Admin.Controllers
{
    /// <summary>
    /// các chức năng liên quan đến khách hàng 
    /// </summary>
    [Authorize]
    public class CustomerController : Controller
    {
       // private const int PAGESIZE = 10;//hard code

        /// <summary>
        /// tên biến sesion lưu điều kiện tìm kiếm khách hàng 
        /// </summary>
        private const string CUSTOMER_SEARCH_INPUT = "CustomerSearchInput";

        /// <summary>
        /// giao diện để nhập đầu vào tìm kiếm và hiển thị kêts quả timf kiếm 
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("CUSTOMER_SEARCH_INPUT");
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
        /// tìm kiếm và trả về kết quả  dưới dạng phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search( PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData("CUSTOMER_SEARCH_INPUT", input);
            return View(result);
        }

        /// <summary>
        /// bổ sung khachs hàng mới 
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung  khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// cập nhật thông tin kh
        /// </summary>
        /// <param name="id">mã KH cần thêm </param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if(model == null)
            
                return RedirectToAction("Index");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //TODO: Kiểm tra tính hợp lệ của dữ liệu và thông báo lỗi nếu dữ liệu không hợp lệ.

            //sử dụng ModelState.IsValid để kiểm soát thong báo lỗi và gửi thông báo lỗi cho view
            try
            {
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "vui long nhập tên khách hàng .");


                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "vui long nhập email khách hàng .");
                else if (!(await PartnerDataService.ValidatelCustomerEmailAsync(data.Email, data.CustomerID)))
                    ModelState.AddModelError(nameof(data.Email), "Email đã tồn tại trong hệ thống .");
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "vui long nhập tỉnh thành khách hàng .");

                //điều chỉnh lại các giá trị dư liệu khác theo quy định quy ước của App
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = "";

                //Yeu cầu lưu dữ liệu và CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Edit", data);
            }
        }
        /// <summary>
        /// hiển thi xác nhận xóa khách hàng 
        /// </summary>
        /// <param name="id">mã KH cần xóa </param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            ViewBag.AllowedDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
            return View(model);
        }

        /// <summary>
        /// đổi mk KH 
        /// </summary>
        /// <param name="id">mã KH cần đổi mk</param>
        /// <returns></returns>
        // GET: hiển thị form
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return RedirectToAction("Index");

            return View("~/Views/Customer/ChangePassword.cshtml", customer);
        }


        // POST: xử lý đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return RedirectToAction("Index");

            ViewData["Title"] = "Đổi mật khẩu khách hàng";

            // validate
            if (string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin");
                return View("~/Views/Customer/ChangePassword.cshtml", customer);
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu không đúng");
                return View("~/Views/Customer/ChangePassword.cshtml", customer);
            }

            //  hash password
            newPassword = CryptHelper.HashMD5(newPassword);

            //  gọi xử lý
            bool result = await PartnerDataService.ChangeCustomerPasswordAsync(id, newPassword);

            if (!result)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại");
                return View("~/Views/Customer/ChangePassword.cshtml", customer);
            }

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Index");
        }
    }
}
