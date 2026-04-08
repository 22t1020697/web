using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.DataLayers.SQLServer;
using SV22T1020697.Models.Partner;
using SV22T1020697.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020697.Admin.Controllers
{
    /// <summary>
    /// các chức năng liên quan đến tài khoản
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        /// <summary>
        /// chức năng đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }
        
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
       
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin đăng nhập");
                return View();
            }

            // CHỈ HASH 1 LẦN Ở ĐÂY
            string hashedPassword = CryptHelper.HashMD5(password);

            // Gọi Service thực tế
            var userAccount = await UserAccountService.AuthorizeAsync(username, hashedPassword);

            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Thông tin đăng nhập không hợp lệ");
                return View();
            }

            // Dữ liệu dùng để ghi vào giấy chứng nhận (principal)
            var userData = new WebUserData
            {
                UserId = userAccount.UserId, 
                UserName = userAccount.Email,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo ?? "nophoto.png",
                Roles = userAccount.RoleNames?.Split(',').ToList() ?? new List<string>()
            };

            var principal = userData.CreatePrincipal();
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// chức năng đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
      // DEBUG ROLE 

        public IActionResult DebugRole()
        {
            var roles = User.Claims
                .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Json(roles);
        }
        public IActionResult Index()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
          
            if (string.IsNullOrWhiteSpace(oldPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "Nhập đầy đủ thông tin");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không đúng");
                return View();
            }
            // Trong hàm ChangePassword [HttpPost]
            string hashedOld = CryptHelper.HashMD5(oldPassword);
            string hashedNew = CryptHelper.HashMD5(newPassword);
            var connStr = _configuration.GetConnectionString("LiteCommerceDB") ?? "";
            // Hoặc an toàn hơn để debug:
            // var connStr = _configuration.GetConnectionString("LiteCommerceDB") ?? throw new Exception("ConnectionString 'LiteCommerceDB' not found!");

            var repo = new EmployeeAccountRepository(connStr);

            var userData = User.GetUserData();
            string userName = userData?.UserName ?? "";

            
            bool result = await repo.ChangePasswordAsync(userName, hashedOld, hashedNew );

            if (!result)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng");
                return View();
            }
            ModelState.Clear();
            ViewBag.Message = "Đổi mật khẩu thành công";
            return View();
            


        }

        /// <summary>
        /// Chức năng đăng ký 
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer data, string confirmPassword)
        {
            if (data.Password != confirmPassword)
            {
                ModelState.AddModelError("Error", "Mật khẩu xác nhận không khớp");
                return View(data);
            }

            data.Password = CryptHelper.HashMD5(data.Password);
            int result = await UserAccountService.RegisterAsync(data);

            if (result == -1)
            {
                ModelState.AddModelError("Error", "Email này đã được sử dụng");
                return View(data);
            }

            return RedirectToAction("Login");
        }
        /// <summary>
        /// Chức năng Quên mật khẩu 
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // Logic gửi mail/reset mật khẩu ở đây
            ViewBag.Message = "Hệ thống đã nhận yêu cầu khôi phục cho: " + email;
            return View();
        }
    }
}
