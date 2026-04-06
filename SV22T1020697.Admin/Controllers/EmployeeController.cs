using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020697.Admin;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.HR;

namespace SV22T1020697.Admin.Controllers
{
    public class EmployeeController : Controller
    {
        // ================= VIEW =================
        [Authorize]
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>("EmployeeSearchInput")
                        ?? new PaginationSearchInput()
                        {
                            Page = 1,
                            PageSize = ApplicationContext.PageSize,
                            SearchValue = ""
                        };
            return View(input);
        }

        [Authorize]
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData("EmployeeSearchInput", input);
            return View(result);
        }

        // ================= CREATE / EDIT =================
        [Authorize]
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            return View("Edit", new Employee() { EmployeeID = 0, IsWorking = true });
        }

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên");
                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã tồn tại");
                if (!ModelState.IsValid) return View("Edit", data);

                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var path = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using var stream = new FileStream(path, FileMode.Create);
                    await uploadPhoto.CopyToAsync(stream);
                    data.Photo = fileName;
                }

                data.Address ??= "";
                data.Phone ??= "";
                data.Photo ??= "nophoto.png";

                if (data.EmployeeID == 0)
                {
                    data.Password = "123456";
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                    await HRDataService.UpdateEmployeeAsync(data);

                return RedirectToAction("Index");
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống:" + ex.Message);
                return View("Edit", data);
            }
        }

        // ================= DELETE =================
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            ViewBag.AllowDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(int id, string confirm = "")
        {
            if (await HRDataService.IsUsedEmployeeAsync(id))
            {
                var model = await HRDataService.GetEmployeeAsync(id);
                ViewBag.AllowDelete = false;
                ModelState.AddModelError("", "Không thể xóa nhân viên này!");
                return View(model);
            }

            await HRDataService.DeleteEmployeeAsync(id);
            return RedirectToAction("Index");
        }

        // ================= ROLE =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null) return RedirectToAction("Index");

            //  Truyền role hiện tại vào View
            ViewBag.Roles = (employee.RoleNames ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return View(employee);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string[]? roles)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null) return RedirectToAction("Index");

            //   Cập nhật RoleNames
            employee.RoleNames = roles != null ? string.Join(",", roles) : "";
            await HRDataService.UpdateEmployeeAsync(employee);

            TempData["Message"] = "Cập nhật quyền thành công!";
            return RedirectToAction("Index");
        }

        // ================= PASSWORD =================
        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            ViewData["EmployeeId"] = id;
            return View("~/Views/Account/ChangePassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int EmployeeID, string OldPassword, string NewPassword, string ConfirmPassword)
        {
            ViewData["EmployeeId"] = EmployeeID;

            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                ModelState.AddModelError("", "Nhập mật khẩu cũ");
                return View("~/Views/Account/ChangePassword.cshtml");
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ModelState.AddModelError("", "Nhập mật khẩu mới");
                return View("~/Views/Account/ChangePassword.cshtml");
            }

            if (NewPassword != ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View("~/Views/Account/ChangePassword.cshtml");
            }

            var currentUser = User.GetUserData();
            if (currentUser == null) return RedirectToAction("Login", "Account");

            // Thay dòng 185 hiện tại bằng dòng này:
            if (!(currentUser.Roles?.Contains("admin") ?? false) && currentUser.UserId != EmployeeID.ToString()) return RedirectToAction("AccessDenied", "Account");

            bool result = await PartnerDataService.ChangeEmployeePasswordAsync(EmployeeID, OldPassword, NewPassword);

            if (result)
            {
                TempData["Message"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Sai mật khẩu cũ hoặc lỗi hệ thống");
            return View("~/Views/Account/ChangePassword.cshtml");
        }
    }
    }