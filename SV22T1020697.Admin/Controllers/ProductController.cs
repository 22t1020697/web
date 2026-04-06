using Microsoft.AspNetCore.Mvc;
using SV22T1020697.Admin;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020697.Admin.Controllers
{
    public class ProductController : Controller
    {
        //private const int PAGESIZE = 10;
        private const string PRODUCT_SEARCH_INPUT = "ProductSearchInput";

        /// <summary>
        /// Trang danh sách sản phẩm
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<SV22T1020697.Models.Catalog.ProductSearchInput>(PRODUCT_SEARCH_INPUT);
            if (input == null)
            {
                input = new SV22T1020697.Models.Catalog.ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }

            // ✅ FIX: dùng await + đúng tên hàm
            ViewBag.Categories = (await CatalogDataService
                .ListCategoriesAsync(new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                })).DataItems;

            ViewBag.Suppliers = (await PartnerDataService
                .ListSuppliersAsync(new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                })).DataItems;

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm sản phẩm
        /// </summary>
        public async Task<IActionResult> Search(SV22T1020697.Models.Catalog.ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);

            ApplicationContext.SetSessionData((string)PRODUCT_SEARCH_INPUT, (object)input);

            return View(result); // hoặc PartialView nếu dùng AJAX
        }

        public IActionResult Detail(int id)
        {
            return View();
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            return View("Edit", new Product()
            {
                ProductID = 0,
                IsSelling = true
            });
        }


        /// <summary>
        /// Hiển thị form cập nhật sản phẩm
        /// </summary>
        public async Task<IActionResult> Edit(int id)
        {
            var model = await CatalogDataService.GetProductAsync(id);

            if (model == null)
                return RedirectToAction("Index");

            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu sản phẩm (Insert + Update)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.ProductID == 0
                ? "Bổ sung sản phẩm"
                : "Cập nhật sản phẩm";

            // Validate
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName),
                    "Vui lòng nhập tên sản phẩm");

            if (data.CategoryID <= 0)
                ModelState.AddModelError(nameof(data.CategoryID),
                    "Vui lòng chọn danh mục");

            if (data.SupplierID <= 0)
                ModelState.AddModelError(nameof(data.SupplierID),
                    "Vui lòng chọn nhà cung cấp");
            if (string.IsNullOrWhiteSpace(data.Unit))
                ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính (ví dụ: Cái, Kg, Bộ...)");

            if (data.Price < 0)
                ModelState.AddModelError(nameof(data.Price), "Giá bán không hợp lệ");
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();

                return View("Edit", data);
            }


            // Upload ảnh
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName; // Gán tên file mới
            }

            // Nếu là thêm mới mà không có ảnh thì dùng ảnh mặc định
            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            // 3. Lưu vào Database
            try
            {
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống: " + ex.Message);
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        /// <summary>
        /// Hiển thị trang xác nhận xóa sản phẩm
        /// </summary>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                return RedirectToAction("Index");
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Xử lý lỗi convert int? sang int bằng cách dùng ?? 0
            var category = await CatalogDataService.GetCategoryAsync(model.CategoryID ?? 0);
            var supplier = await PartnerDataService.GetSupplierAsync(model.SupplierID ?? 0);

            ViewBag.CategoryName = category?.CategoryName ?? "Chưa xác định";
            ViewBag.SupplierName = supplier?.SupplierName ?? "Chưa xác định";
            ViewBag.AllowDelete = !(await CatalogDataService.IsUsedProductAsync(id));

            return View(model);
        }

        // CHỈ GIỮ LẠI MỘT HÀM ListAttribute DUY NHẤT DƯỚI ĐÂY
        // Xóa tất cả các hàm ListAttribute khác đang có trong file này



        // ===== ATTRIBUTE =====

        public IActionResult ListAttribute(int id)
        {
            return View();
        }

        // ❌ trùng → nên xóa nếu không dùng
        // public IActionResult ListAttributes(int id)

        public IActionResult CreateAttribute(int id)
        {
            return View("EditAttribute");
        }

        public IActionResult EditAttribute(int id, int attributeId)
        {
            return View();
        }

        public IActionResult DeleteAttribute(int id, int attributeId)
        {
            return View();
        }

        // ===== PHOTOS =====

        public IActionResult ListPhotos(int id)
        {
            return View();
        }

        public IActionResult CreatePhotos(int id)
        {
            return View();
        }

        public IActionResult EditPhotos(int id, int photoId)
        {
            return View();
        }

        // ❌ trùng → nên giữ 1 cái thôi
        // public IActionResult EditPhoto(int id, int photoId)
    }
}
