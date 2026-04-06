using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;

namespace SV22T1020697.Shop.Controllers
{
    public class ProductController : Controller
    {
        public async Task<IActionResult> Index(string searchValue = "", int categoryId = 0,
                                             decimal minPrice = 0, decimal maxPrice = 0, int page = 1)
        {
            int pageSize = 12;

            // 1. Lấy danh sách danh mục (Sửa lỗi .DataItems ở đây)
            var categoryInput = new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = "" };
            var categoryResult = await CatalogDataService.ListCategoriesAsync(categoryInput);

            // Gán DataItems vào ViewBag để Layout hiển thị
            ViewBag.Categories = categoryResult.DataItems;

            // 2. Thiết lập đầu vào tìm kiếm sản phẩm
            var productInput = new ProductSearchInput()
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue ?? "",
                CategoryID = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };

            // 3. Lấy dữ liệu sản phẩm
            var data = await CatalogDataService.ListProductsAsync(productInput);

            // Lưu lại trạng thái để hiển thị trên form/layout
            ViewBag.CurrentSearchValue = searchValue;
            ViewBag.CurrentCategoryId = categoryId;

            return View(data);
        }

        public async Task<IActionResult> Detail(int id)
        {
            // Tương tự, nạp lại Categories cho Layout khi xem chi tiết
            var categoryInput = new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = "" };
            var categoryResult = await CatalogDataService.ListCategoriesAsync(categoryInput);
            ViewBag.Categories = categoryResult.DataItems;

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            return View(product);
        }
    }
}