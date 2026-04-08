using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SV22T1020697.Admin;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020697.Admin.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH_INPUT = "ProductSearchInput";
        private readonly IWebHostEnvironment _environment;

        public ProductController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_INPUT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0
                };
            }

            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            return View(input);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);
            return PartialView("Search", result);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung sản phẩm";
            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            return View("Edit", new Product() { ProductID = 0, IsSelling = true, Photo = "nophoto.png" });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.Title = "Cập nhật sản phẩm";
            ViewBag.Categories = await SelectListHelper.Categories();
            ViewBag.Suppliers = await SelectListHelper.Suppliers();

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            // 1. Chuẩn hóa dữ liệu
            data.ProductName = data.ProductName?.Trim() ?? "";
            data.Unit = data.Unit?.Trim() ?? "";
            data.ProductDescription = data.ProductDescription?.Trim() ?? "";

            // 2. Gỡ bỏ lỗi Validation cho các trường không bắt buộc
            ModelState.Remove(nameof(data.ProductDescription));
            ModelState.Remove(nameof(data.Photo));

            // 3. Kiểm tra tính hợp lệ
            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên sản phẩm không được rỗng");
            if (data.CategoryID <= 0)
                ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");
            if (data.SupplierID <= 0)
                ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");
            if (string.IsNullOrWhiteSpace(data.Unit))
                ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được rỗng");
            if (data.Price <= 0)
                ModelState.AddModelError(nameof(data.Price), "Giá sản phẩm phải lớn hơn 0");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung sản phẩm" : "Cập nhật sản phẩm";
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();
                return View("Edit", data);
            }

            // 4. Xử lý Ảnh
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(_environment.WebRootPath, "images", "products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = $"products/{fileName}";
            }
            else if (data.ProductID == 0 && string.IsNullOrEmpty(data.Photo))
            {
                data.Photo = "nophoto.png";
            }

            // 5. Lưu vào CSDL
            try
            {
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                TempData["SuccessMessage"] = "Lưu thông tin sản phẩm thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                ViewBag.Categories = await SelectListHelper.Categories();
                ViewBag.Suppliers = await SelectListHelper.Suppliers();
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await CatalogDataService.DeleteProductAsync(id);
                    TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
                    return RedirectToAction("Index");
                }
                catch
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm vì đã có dữ liệu liên quan.";
                    return RedirectToAction("Index");
                }
            }

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.AllowDelete = true; // Hiện nút xác nhận xóa
            ViewBag.CategoryName = (await CatalogDataService.GetCategoryAsync(model.CategoryID ?? 0))?.CategoryName;
            ViewBag.SupplierName = (await CatalogDataService.GetSupplierAsync(model.SupplierID ?? 0))?.SupplierName;

            return View(model);
        }

        // =========================================================================
        // QUẢN LÝ ẢNH PHỤ (PHOTOS)
        // =========================================================================

        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung ảnh";
            return View("EditPhoto", new ProductPhoto() { ProductID = id, PhotoID = 0, IsHidden = false, DisplayOrder = 1 });
        }

        public async Task<IActionResult> EditPhoto(int id, int photoId)
        {
            ViewBag.Title = "Thay đổi ảnh";
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View("EditPhoto", model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            data.Description = data.Description?.Trim() ?? ""; // Tránh lỗi NULL DB
            ModelState.Remove(nameof(data.Photo));

            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(_environment.WebRootPath, "images", "products");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = $"products/{fileName}";
            }
            else if (data.PhotoID == 0)
            {
                ModelState.AddModelError("Photo", "Vui lòng chọn file ảnh để upload");
            }

            if (!ModelState.IsValid) return View("EditPhoto", data);

            try
            {
                if (data.PhotoID == 0)
                    await CatalogDataService.AddPhotoAsync(data);
                else
                    await CatalogDataService.UpdatePhotoAsync(data);

                TempData["SuccessMessage"] = "Lưu ảnh phụ thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi lưu ảnh: " + ex.Message;
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        public async Task<IActionResult> DeletePhoto(int id, int photoId)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    TempData["SuccessMessage"] = "Xóa ảnh phụ thành công.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa ảnh: " + ex.Message;
                }
                return RedirectToAction("Edit", new { id = id });
            }
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View(model);
        }

        // =========================================================================
        // QUẢN LÝ THUỘC TÍNH (ATTRIBUTES)
        // =========================================================================

        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";
            return View("EditAttribute", new ProductAttribute() { ProductID = id, AttributeID = 0, DisplayOrder = 1 });
        }

        public async Task<IActionResult> EditAttribute(int id, int attributeId)
        {
            ViewBag.Title = "Thay đổi thuộc tính";
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View("EditAttribute", model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            data.AttributeName = data.AttributeName?.Trim() ?? "";
            data.AttributeValue = data.AttributeValue?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được trống");

            if (!ModelState.IsValid) return View("EditAttribute", data);

            try
            {
                if (data.AttributeID == 0)
                    await CatalogDataService.AddAttributeAsync(data);
                else
                    await CatalogDataService.UpdateAttributeAsync(data);

                TempData["SuccessMessage"] = "Lưu thuộc tính thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi lưu thuộc tính: " + ex.Message;
            }

            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        public async Task<IActionResult> DeleteAttribute(int id, int attributeId)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    TempData["SuccessMessage"] = "Xóa thuộc tính thành công.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi xóa thuộc tính: " + ex.Message;
                }
                return RedirectToAction("Edit", new { id = id });
            }
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View(model);
        }
    }
}