using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;
using SV22T1020697.Admin;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020697.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Quản lý danh sách loại hàng  (Category).
    /// </summary>
    public class CategoryController : Controller
    {
        ///private const int PAGESIZE = 10;
        private const string CATEGORY_SEARCH_INPUT = "CategorySearchInput";

        /// <summary>
        /// Hiển thị danh sách loại hàng 
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH_INPUT);
            if (input == null)
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm loại hàng 
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await CatalogDataService.ListCategoriesAsync(input);

            ApplicationContext.SetSessionData(CATEGORY_SEARCH_INPUT, input);

            return View(result);
        }

        /// <summary>
        /// Thêm mới loại hàng 
        /// </summary>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng ";

            var model = new Category()
            {
                CategoryID = 0
            };

            return View("Edit", model);
        }

        /// <summary>
        /// Hiển thị biểu mẫu cập nhật loại hàng 
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật loại hàng ";

            var model = await CatalogDataService.GetCategoryAsync(id);

            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu loại hàng   (Insert + Update)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0
                ? "Bổ sung loại hàng "
                : "Cập nhật loại hàng ";

            try
            {
                // Validate dữ liệu
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                    ModelState.AddModelError(nameof(data.CategoryName),
                        "Vui lòng nhập tên loại hàng ");

                // Chuẩn hóa dữ liệu null
                if (string.IsNullOrEmpty(data.Description))
                    data.Description = "";

                if (!ModelState.IsValid)
                    return View("Edit", data);


                // Lưu database
                if (data.CategoryID == 0)
                    await CatalogDataService.AddCategoryAsync(data);
                else
                    await CatalogDataService.UpdateCategoryAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("Error",
                    "Hệ thống đang bận vui lòng thử lại sau");

                return View("Edit", data);
            }
        }

        /// <summary>
        /// Hiển thị trang xác nhận xóa danh mục
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetCategoryAsync(id);

            if (model == null)
                return RedirectToAction("Index");

            ViewBag.AllowDelete =
                !(await CatalogDataService.IsUsedCategoryAsync(id));

            return View(model);
        }
    }
}