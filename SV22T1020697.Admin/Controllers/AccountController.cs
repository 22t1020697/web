using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020697.DataLayers.SQLServer;
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
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin đăng nhập");
                return View();
            }
            password = CryptHelper.HashMD5(password);
            //TODO: kiểm tra username và password có đúng ko?
            //ví dụ:
            //
            //var userAccount = await SecurityDataService.EmployeeAuthorizeAsync(username, password);

            //giả lập:
            var userAccount = new UserAccount
            {
                UserId = "NV01",
                UserName = username,
                DisplayName = username,
                Email = username,
                Photo = "avatar.png",
                RoleNames = "admin,datamanager"
            };
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Thông tin đăng nhập không hợp lệ");
                return View();
            }
            //duữ liệu sẽ dùng để "ghi" vào giấy chứng nhận (principal)
            var userData = new WebUserData
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',').ToList()
            };
            //thiết lập phiên đăng nhập ( cấp giấy chứng nhận)
            await HttpContext.SignInAsync
                (
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    userData.CreatePrincipal()

                );
            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// chức năng đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
        // ================= DEBUG ROLE =================

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

            var connStr = _configuration.GetConnectionString("LiteCommerceDB") ?? "";
            // Hoặc an toàn hơn để debug:
            // var connStr = _configuration.GetConnectionString("LiteCommerceDB") ?? throw new Exception("ConnectionString 'LiteCommerceDB' not found!");

            var repo = new EmployeeAccountRepository(connStr);

            var userData = User.GetUserData();
            string userName = userData?.UserName ?? "";

            // 👉 Repo sẽ tự hash
            bool result = await repo.ChangePasswordAsync(userName, oldPassword, newPassword);

            if (!result)
            {
                ModelState.AddModelError("", "Mật khẩu cũ không đúng");
                return View();
            }

            ViewBag.Message = "Đổi mật khẩu thành công";
            return View();
            

        }

    }
}
