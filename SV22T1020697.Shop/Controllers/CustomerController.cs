using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.SQLServer;
using SV22T1020697.Models.Partner;
using SV22T1020697.Models.Sales;
using SV22T1020697.Shop; // Đảm bảo namespace này chứa ShoppingCartHelper

namespace SV22T1020697.Shop.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerAccountRepository _accountRepo;
        private readonly string _connectionString;

        public CustomerController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LiteCommerceDB")
                                    ?? throw new Exception("Chưa cấu hình LiteCommerceDB");
            _accountRepo = new CustomerAccountRepository(_connectionString);
        }

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

            string md5Password = CryptHelper.HashMD5(password);
            var customer = await _accountRepo.LoginAsync(email, md5Password);

            if (customer != null)
            {
                // 1. Lưu thông tin định danh vào Session
                HttpContext.Session.SetInt32("CustomerID", customer.CustomerID);
                HttpContext.Session.SetString("CustomerName", customer.CustomerName ?? "");

                // 2. Đồng bộ giỏ hàng: Chỉ lấy từ Database nạp vào Session (Ghi đè hoàn toàn)
                await SyncCartAfterLogin(customer.CustomerID);

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

            if (await _accountRepo.ExistsAsync(email))
            {
                ViewBag.ErrorMessage = "Email này đã được đăng ký bởi tài khoản khác.";
                return View("Login");
            }

            var newCustomer = new Customer
            {
                CustomerName = email,
                ContactName = email,
                Email = email,
                Phone = phone ?? "",
                Password = CryptHelper.HashMD5(password),
                IsLocked = false
            };

            try
            {
                int newId = await _accountRepo.AddAsync(newCustomer);
                if (newId > 0)
                {
                    HttpContext.Session.SetInt32("CustomerID", newId);
                    HttpContext.Session.SetString("CustomerName", newCustomer.CustomerName);

                    // Khách mới đăng ký thì giỏ hàng mặc định trống
                    ShoppingCartHelper.ClearCart();

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Lỗi hệ thống: " + ex.Message;
                return View("Login");
            }

            ViewBag.ErrorMessage = "Có lỗi xảy ra trong quá trình tạo tài khoản.";
            return View("Login");
        }

        public async Task<IActionResult> Profile()
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null) return RedirectToAction("Login");

            var customer = await _accountRepo.GetAsync(customerId.Value);
            if (customer == null) return RedirectToAction("Logout");

            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfo(string customerName, string phone, string email, string address)
        {
            var customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId == null) return RedirectToAction("Login");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"UPDATE Customers 
                                   SET CustomerName = @name, Phone = @phone, Email = @email, Address = @address 
                                   WHERE CustomerID = @id";
                    await connection.ExecuteAsync(sql, new { name = customerName, phone, email, address, id = customerId });
                }

                HttpContext.Session.SetString("CustomerName", customerName);
                TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi cập nhật: " + ex.Message;
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

            var customer = await _accountRepo.GetAsync(customerId.Value);
            if (customer == null || customer.Password != CryptHelper.HashMD5(oldPassword))
            {
                TempData["Error"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction("Profile");
            }

            string md5NewPassword = CryptHelper.HashMD5(newPassword);
            bool success = await _accountRepo.ChangePasswordAsync(customerId.Value, md5NewPassword);

            if (success)
                TempData["Success"] = "Đổi mật khẩu thành công!";
            else
                TempData["Error"] = "Không thể đổi mật khẩu, vui lòng thử lại.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// ĐỒNG BỘ: Chỉ lấy dữ liệu từ Database nạp vào Session (Ghi đè hoàn toàn Session cũ)
        /// </summary>
        private async Task SyncCartAfterLogin(int customerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                // 1. Lấy dữ liệu giỏ hàng từ DB (Kết hợp bảng Products để lấy thông tin hiển thị)
                string sqlDbCart = @"
                    SELECT c.ProductID, c.Quantity, p.ProductName, p.Photo, p.Unit, p.Price as SalePrice
                    FROM Cart c
                    JOIN Products p ON c.ProductID = p.ProductID
                    WHERE c.CustomerID = @id";

                var dbItems = await connection.QueryAsync<CartItem>(sqlDbCart, new { id = customerId });

                // 2. Chuyển đổi kết quả từ DB thành danh sách
                var cartFromDb = dbItems.ToList();

                // 3. GHI ĐÈ GIỎ HÀNG TRONG SESSION:
                // Sử dụng đúng key "ShoppingCart" (theo ShoppingCartHelper) để ghi đè dữ liệu cũ
                ApplicationContext.SetSessionData("ShoppingCart", cartFromDb);
            }
        }

        public IActionResult Logout()
        {
            // Xóa sạch Session (bao gồm CustomerID và ShoppingCart)
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}