using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.DataLayers.SQLServer;
using SV22T1020697.Models.Partner;
using SV22T1020697.Models.Sales;
using SV22T1020697.Shop;
using System.Security.Cryptography;


namespace SV22T1020697.Shop.Controllers
{
    public class CustomerController : Controller
    {
            [HttpGet]
            public IActionResult Login() => View();

        [HttpPost]
            public async Task<IActionResult> Login(string email, string password)
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
                    return View();
                }

                // ĐÚNG: Mã hóa ở Controller để tránh lỗi tham chiếu vòng (Circular Dependency)
                string md5Password = CryptHelper.HashMD5(password);

                // ĐÚNG: Gọi qua Service, không gọi qua Repo
                var customer = await UserAccountService.AuthorizeCustomerAsync(email, md5Password);

                if (customer != null)
                {
                    HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                    HttpContext.Session.SetString("CustomerName", customer.CustomerName ?? "");
                    SyncCartAfterLogin(customer.CustomerID);
                    return RedirectToAction("Index", "Home");
                }

                ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác.";
                return View();
            }
        [HttpGet]
        public IActionResult Register() => View("Login");

        [HttpPost]
        public async Task<IActionResult> Register(string email, string phone, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Vui lòng điền đầy đủ Email và Mật khẩu.";
                return View("Login");
            }

            // ĐÚNG: Mã hóa mật khẩu trước khi gửi đi
            string md5Password = CryptHelper.HashMD5(password);

            // ĐÚNG: Đóng gói dữ liệu và gửi xuống Service xử lý nghiệp vụ
            var newCustomer = new Customer
            {
                CustomerName = email,
                ContactName = email,
                Email = email,
                Phone = phone ?? "",
                Password = md5Password, // Mật khẩu đã mã hóa
                IsLocked = false
            };

          
            int newId = await UserAccountService.RegisterAsync(newCustomer);

            if (newId > 0)
            {
                HttpContext.Session.SetInt32("CustomerID", newId);
                HttpContext.Session.SetString("CustomerName", newCustomer.CustomerName);
                ShoppingCartHelper.ClearCart();
                return RedirectToAction("Index", "Home");
            }
            else if (newId == -1)
            {
                ViewBag.ErrorMessage = "Email này đã được đăng ký bởi tài khoản khác.";
            }
            else
            {
                ViewBag.ErrorMessage = "Có lỗi xảy ra trong quá trình tạo tài khoản.";
            }

            return View("Login");
        }
           
        public async Task<IActionResult> Profile()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null) return RedirectToAction("Login");

            // Lấy thông tin thông qua Service
            var customer = await UserAccountService.GetCustomerAsync(customerId.Value);
            if (customer == null) return RedirectToAction("Logout");

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo(Customer data)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null) return RedirectToAction("Login");

            data.CustomerID = customerId.Value;

            // Business Layer thực hiện Update, không viết SQL ở đây
            bool success = await UserAccountService.UpdateProfileAsync(data);
            if (success)
            {
                HttpContext.Session.SetString("CustomerName", data.CustomerName);
                TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                TempData["Error"] = "Cập nhật thất bại.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null) return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Xác nhận mật khẩu mới không khớp.";
                return RedirectToAction("Profile");
            }

            // ĐỒNG BỘ: Mã hóa mật khẩu giống hệt như lúc Login và Register
            string md5OldPassword = CryptHelper.HashMD5(oldPassword);
            string md5NewPassword = CryptHelper.HashMD5(newPassword);

            // Truyền mật khẩu ĐÃ MÃ HÓA vào Service
            bool success = await UserAccountService.ChangePasswordAsync(customerId.Value, md5OldPassword, md5NewPassword);

            if (success)
                TempData["Success"] = "Đổi mật khẩu thành công!";
            else
                TempData["Error"] = "Mật khẩu cũ không đúng hoặc có lỗi xảy ra.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// ĐỒNG BỘ: Chỉ lấy dữ liệu từ Database nạp vào Session thông qua SalesDataService
        /// </summary>
        private void SyncCartAfterLogin(int customerId)
        {
            // Gọi hàm ListCartItems đã viết ở SalesDataService trong bài trước
            var dbItems = SalesDataService.ListCartItems(customerId);

            // Ghi đè vào Session bằng helper
            ApplicationContext.SetSessionData("ShoppingCart", dbItems);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}