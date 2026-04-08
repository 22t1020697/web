using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020697.Admin;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Sales;
using System;
using System.Threading.Tasks;

namespace SV22T1020697.Admin.Controllers
{
    /// <summary>
    /// cung cấp các chức năng liên quan đến nghiệp vụ bán hàng 
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
        private const string ORDER_SEARCH_INPUT = "OrderSearchInput";
        private const string PRODUCT_SEARCH_INPUT = "ProductSearchOrder";

        /// <summary>
        /// nhập đầu vào tìm kiếm đơn hàng và hiển thị kết quả tìm kiếm 
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH_INPUT);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }

        /// <summary>
        /// tìm kiếm và hiển thị ds đơn hàng 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH_INPUT, input);
            return PartialView("Search", result);
        }

        /// <summary>
        /// Tìm sản phẩm khi tạo đơn
        /// </summary>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH_INPUT, input);
            return PartialView("_SearchProduct", result);
        }

        /// <summary>
        /// giao diện thực hiện chức năng lập đơn hàng mới 
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH_INPUT);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 5,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0
                };
            }
            return View(input);
        }

        /// <summary>
        /// thêm vào giỏ hàng hoặc đơn hàng 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productId = 0, int quantity = 0, decimal price = 0)
        {
            try
            {
                if (quantity <= 0) return Json(new ApiResult(0, "Số lượng phải lớn hơn 0"));

                var product = await CatalogDataService.GetProductAsync(productId);
                if (product == null)
                {
                    return Json(new ApiResult(0, "mặt hàng ko tồn tại"));
                }
                if (!product.IsSelling) // Đã sửa lại logic: Nếu KHÔNG đang bán thì báo lỗi
                {
                    return Json(new ApiResult(0, "mặt hàng này đã ngưng bán"));
                }

                var item = new OrderDetailViewInfo()
                {
                    ProductID = productId,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    Photo = product.Photo ?? "nophoto.png",
                    Quantity = quantity,
                    SalePrice = price
                };
                ShoppingCartHelper.AddItemToCart(item);
                return Json(new ApiResult(1, "Thêm thành công"));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        /// <summary>
        /// xóa Mh ra khỏi giỏ hàng hoặc đơn hàng đã tồn tại 
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(productId);
                return Json(new ApiResult(1, ""));
            }

            var item = ShoppingCartHelper.GetCartItem(productId);
            return PartialView("DeleteCartItem", item);
        }

        /// <summary>
        /// xóa giỏ hàng 
        /// </summary>
        /// <returns></returns>
        public ActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }
            return PartialView("ClearCart");
        }

        /// <summary>
        /// hiênr thị thông tin của 1 đơn hàng và điều hướng đến các chức năng
        /// xử lý đơn hàng
        /// </summary>
        /// <param name="id">mã của đơn hàng </param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");
            return View(order);
        }

        /// <summary>
        /// cập nhật thông tin của 1 MH trong giỏ hàng 
        /// </summary>
        /// <param name="productId">mã Mh cần xử lý</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult EditCartItem(int productId = 0)
        {
            var item = ShoppingCartHelper.GetCartItem(productId);
            if (item == null) return Content("Không tìm thấy mặt hàng");
            return PartialView("EditCartItem", item);
        }

        public IActionResult UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            if (quantity <= 0) return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            ShoppingCartHelper.UpdateCartItem(productID, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// duyệt chấp nhận đơn hàng
        /// </summary>
        /// <summary>
        /// Duyệt chấp nhận đơn hàng
        /// </summary>
        [HttpGet]
        public IActionResult Accept(int id)
        {
            // Quan trọng: Chỉ truyền id, không lấy OrderViewInfo ở đây
            return PartialView("Accept", id);
        }

        [HttpPost]
        public async Task<IActionResult> Accept(int id, string _ = "") // thêm tham số giả để tránh trùng signature nếu cần
        {
            int employeeId = 2; // Giả sử ID người dùng hiện tại
            bool result = await SalesDataService.AcceptOrderAsync(id, employeeId);
            if (result)
                return Json(new { code = 1, message = "Duyệt đơn hàng thành công" });

            return Json(new { code = 0, message = "Không thể duyệt đơn hàng này" });
        }

        /// <summary>
        /// Chuyển hàng 
        /// </summary>
        /// <summary>
        /// Xử lý chuyển giao hàng (POST)
        /// </summary>
        /// <summary>
        /// Xử lý chuyển giao hàng (POST) - Hỗ trợ chọn ngẫu nhiên
        /// </summary>
        /// <summary>
        /// Hiển thị giao diện chọn người giao hàng (GET)
        /// </summary>
        /// <summary>
        /// Hiển thị giao diện chọn người giao hàng (GET)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var searchInput = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 100,
                SearchValue = ""
            };

            var shipperResult = await PartnerDataService.ListShippersAsync(searchInput);

            // Đã đổi thành .DataItems theo đúng class PagedResult của bạn
            ViewBag.Shippers = shipperResult.DataItems;

            return PartialView("Shipping", id);
        }

        /// <summary>
        /// Xử lý chuyển giao hàng (POST) - Hỗ trợ Random
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            // 1. Nếu chưa chọn Shipper (shipperID = 0), ta sẽ Random
            if (shipperID <= 0)
            {
                var searchInput = new PaginationSearchInput() { Page = 1, PageSize = 100, SearchValue = "" };
                var shipperResult = await PartnerDataService.ListShippersAsync(searchInput);

                // Đã đổi thành .DataItems và thêm dấu ? để chống lỗi null (Dereference null)
                var shippers = shipperResult.DataItems?.ToList();

                if (shippers != null && shippers.Count > 0)
                {
                    // Random bốc thăm
                    Random rnd = new Random();
                    int randomIndex = rnd.Next(shippers.Count);

                    shipperID = shippers[randomIndex].ShipperID;
                }
                else
                {
                    return Json(new { code = 0, message = "Lỗi: Hệ thống hiện chưa có người giao hàng nào!" });
                }
            }

            // 2. Cập nhật vào Database
            bool result = await SalesDataService.ShipOrderAsync(id, shipperID);

            // 3. Trả về thông báo
            if (result)
            {
                return Json(new { code = 1, message = $"Chuyển giao hàng thành công! (Mã Shipper: {shipperID})" });
            }
            else
            {
                return Json(new { code = 0, message = "Không thể chuyển giao hàng do trạng thái đơn hàng không hợp lệ!" });
            }
        }
        /// <summary>
        /// kết thúc thành công đơn hàng 
        /// </summary>
        /// <summary>
        /// Hiển thị giao diện xác nhận hoàn tất đơn hàng (GET)
        /// </summary>
        [HttpGet]
        public IActionResult Finish(int id)
        {
            // Chỉ cần truyền ID của đơn hàng sang PartialView để nó gắn vào nút "Chấp nhận"
            return PartialView("Finish", id);
        }

        /// <summary>
        /// Xử lý cập nhật trạng thái đơn hàng thành Hoàn tất (POST)
        /// </summary>
        /// <summary>
        /// Xử lý cập nhật trạng thái đơn hàng thành Hoàn tất (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Finish(int id, IFormCollection ignores)
        {
            // Đã đổi thành CompleteOrderAsync theo đúng hàm trong SalesDataService của bạn
            bool result = await SalesDataService.CompleteOrderAsync(id);

            // Trả kết quả về cho giao diện (JSON)
            if (result)
            {
                return Json(new
                {
                    code = 1,
                    message = "Xác nhận đơn hàng đã giao thành công!"
                });
            }
            else
            {
                return Json(new
                {
                    code = 0,
                    message = "Không thể hoàn tất đơn hàng do trạng thái hiện tại không hợp lệ (Đơn hàng phải đang ở trạng thái 'Đang giao hàng')!"
                });
            }
        }
        /// <summary>
        /// từ chối đơn hàng 
        /// </summary>
        public async Task<IActionResult> Reject(int id)
        {
            if (Request.Method == "POST")
            {
                int employeeId = 1;
                bool result = await SalesDataService.RejectOrderAsync(id, employeeId);
                if (result)
                    return Json(new ApiResult(1, "Đã từ chối đơn hàng"));

                return Json(new ApiResult(0, "Xử lý thất bại"));
            }
            return PartialView("Reject", id);
        }

        /// <summary>
        /// hủy đơn hàng 
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            if (Request.Method == "POST")
            {
                bool result = await SalesDataService.CancelOrderAsync(id);
                if (result)
                    return Json(new ApiResult(1, "Đã hủy đơn hàng"));

                return Json(new ApiResult(0, "Xử lý thất bại"));
            }
            return PartialView("Cancel", id);
        }

        /// <summary>
        /// tạo đơn hàng 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            if (customerID <= 0 || string.IsNullOrEmpty(province) || string.IsNullOrEmpty(address))
                return Json(new ApiResult(0, "Vui lòng nhập đầy đủ thông tin khách hàng và địa chỉ"));

            var cart = ShoppingCartHelper.GetShoppingCart();
            if (cart.Count == 0)
                return Json(new ApiResult(0, "giỏ hàng đang trống"));

            //lập đơn hàng và ghi chi tiết của đơn hàng
            int orderID = await SalesDataService.AddOrderAsync(customerID, province, address);
            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail()
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                });
            }

            // Xóa giỏ hàng sau khi tạo thành công
            ShoppingCartHelper.ClearCart();

            return Json(new ApiResult(1, "tạo đơn hàng thành công"));
        }

        /// <summary>
        /// xóa đơn hàng 
        /// </summary>
        /// <param name="id">mã đơn hàng cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                bool result = await SalesDataService.DeleteOrderAsync(id);
                if (result)
                    return Json(new ApiResult(1, "Xóa đơn hàng thành công"));

                return Json(new ApiResult(0, "Không thể xóa đơn hàng này (có thể do trạng thái không cho phép)"));
            }
            return PartialView("Delete", id);
        }

        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return PartialView("ShowCart", cart);
        }

        /// <summary>
        /// Lấy danh sách khách hàng dưới dạng JSON để phục vụ tìm kiếm nhanh (Ajax)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomers(string searchValue = "")
        {
            var input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 30,
                SearchValue = searchValue ?? ""
            };
            var result = await PartnerDataService.ListCustomersAsync(input);
            var list = result.DataItems.Select(c => new
            {
                id = c.CustomerID,
                name = c.CustomerName,
                phone = c.Phone,
                address = c.Address,
                province = c.Province
            });
            return Json(list);
        }
        
    
    }
}