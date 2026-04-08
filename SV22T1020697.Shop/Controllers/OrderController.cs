using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Sales;
using SV22T1020697.Models.Common;
using SV22T1020697.Shop;

namespace SV22T1020697.Shop.Controllers
{
    public class OrderController : Controller
    {
        // Không còn cần IConfiguration và SqlConnection ở đây nữa, 
        // BusinessLayer đã lo việc kết nối CSDL.
        public OrderController()
        {
        }

        /// <summary>
        /// Hiển thị trang xác nhận đơn hàng
        /// </summary>
        public async Task<IActionResult> Index(string selectedIds, int? productId, int quantity = 1)
        {
            var model = new List<CartItem>();

            // 1. Mua ngay (ở trang chi tiết sản phẩm)
            if (productId.HasValue)
            {
             
                var product = await CatalogDataService.GetProductAsync(productId.Value);

                if (product != null)
                {
                    model.Add(new CartItem
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName ?? "Sản phẩm",
                        Photo = string.IsNullOrEmpty(product.Photo) ? "nophoto.png" : product.Photo,
                        Unit = product.Unit ?? "",
                        Quantity = quantity,
                        SalePrice = product.Price
                    });
                    ViewBag.SelectedIds = product.ProductID.ToString();
                }
            }
            // 2. Giỏ hàng (Lọc các sản phẩm được chọn)
            else if (!string.IsNullOrEmpty(selectedIds))
            {
                var cart = ShoppingCartHelper.GetShoppingCart();
                var ids = selectedIds.Split(',')
                                     .Where(x => int.TryParse(x, out _))
                                     .Select(int.Parse)
                                     .ToList();

                model = cart.Where(m => ids.Contains(m.ProductID)).ToList();
                ViewBag.SelectedIds = selectedIds;
            }
            else
            {
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin khách hàng đang đăng nhập
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId.HasValue)
            {
                // Gọi đúng Service và thêm 'await' vì đây là hàm Async
                var customer = await PartnerDataService.GetCustomerAsync(customerId.Value);
                ViewBag.Customer = customer;
            }

            ViewBag.ProvinceList = await SelectListHelper.Provinces() ?? new List<SelectListItem>();
            return View(model);
        }

        /// <summary>
        /// Xử lý lưu đơn hàng và dọn dẹp giỏ hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryName, string deliveryProvince, string deliveryAddress, string selectedIds)
        {
            if (string.IsNullOrEmpty(selectedIds)) return RedirectToAction("Index", "Cart");

            var ids = selectedIds.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(int.Parse).ToList();
            var allCart = ShoppingCartHelper.GetShoppingCart();
            var checkoutItems = allCart.Where(m => ids.Contains(m.ProductID)).ToList();

            if (checkoutItems.Count == 0 && !selectedIds.Contains(","))
            {
                return RedirectToAction("Index", "Cart");
            }

            if (string.IsNullOrWhiteSpace(deliveryName) || string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin giao hàng.");
                ViewBag.ProvinceList = await SelectListHelper.Provinces() ?? new List<SelectListItem>();
                ViewBag.SelectedIds = selectedIds;
                return View("Index", checkoutItems);
            }

            try
            {
                int? customerId = HttpContext.Session.GetInt32("CustomerID");
                if (!customerId.HasValue) return RedirectToAction("Login", "Customer");

                // 1. Lưu đơn hàng thông qua BusinessLayer
                int orderId = await SalesDataService.AddOrderAsync(customerId.Value, deliveryProvince, deliveryAddress);

                // 2. Lưu chi tiết đơn hàng thông qua BusinessLayer
                foreach (var item in checkoutItems)
                {
                    var detail = new OrderDetail
                    {
                        OrderID = orderId,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    };
                    await SalesDataService.AddDetailAsync(detail);
                }

                // 3. Xóa các mặt hàng đã thanh toán khỏi Database Cart
                // Bằng cách lấy giỏ hàng DB hiện tại, loại bỏ các item vừa mua và Sync lại.
                var dbCartItems = SalesDataService.ListCartItems(customerId.Value);
                var remainingDbCart = dbCartItems.Where(c => !ids.Contains(c.ProductID)).ToList();
                SalesDataService.SyncCart(customerId.Value, remainingDbCart);

                // 4. Dọn dẹp session giỏ hàng cục bộ
                foreach (var id in ids)
                {
                    ShoppingCartHelper.RemoveItemFromCart(id);
                }

                return RedirectToAction(nameof(Completed));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đặt hàng: " + ex.Message);
                ViewBag.ProvinceList = await SelectListHelper.Provinces();
                ViewBag.SelectedIds = selectedIds;
                return View("Index", checkoutItems);
            }
        }

        public IActionResult Completed() => View();

        public async Task<IActionResult> History()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue) return RedirectToAction("Login", "Customer");

            // Khởi tạo đầu vào tìm kiếm (lấy hết đơn hàng của khách hàng)
            var searchInput = new OrderSearchInput
            {
                Page = 1,
                PageSize = 9999, // Lấy số lượng lớn để hiển thị đủ lịch sử
                SearchValue = ""
            };

            var pagedResult = await SalesDataService.ListOrdersAsync(searchInput);

                    var orders = pagedResult.DataItems
                .Where(o => o.CustomerID == customerId.Value)
                .Select(o => new OrderItemModel
                {
                OrderID = o.OrderID,
                CustomerName = o.CustomerName ?? "",
                OrderTime = o.OrderTime,
                Status = (int)o.Status, 
                TotalAmount = o.TotalAmount,
                DeliveryAddress = o.DeliveryAddress ?? "",
                DeliveryProvince = o.DeliveryProvince ?? ""
            })
            .ToList();

            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
           
            var orderData = await SalesDataService.GetOrderAsync(id);
            if (orderData == null) return RedirectToAction("History");

               var orderModel = new OrderItemModel
            {
                OrderID = orderData.OrderID,
                OrderTime = orderData.OrderTime,
                CustomerName = orderData.CustomerName ?? "",
                DeliveryAddress = orderData.DeliveryAddress ?? "",
                DeliveryProvince = orderData.DeliveryProvince ?? "",
                Status = (int)orderData.Status,
                 
                TotalAmount = orderData.TotalAmount
            };

            // 3. Lấy chi tiết sản phẩm
            var detail = await SalesDataService.ListDetailsAsync(id);

            // 4. Truyền dữ liệu
            ViewBag.Order = orderModel;

            var cartModel = detail.Select(d => new CartItem
            {
                ProductID = d.ProductID,
                ProductName = d.ProductName,
                Photo = d.Photo,
                Unit = d.Unit,
                Quantity = d.Quantity,
                SalePrice = d.SalePrice
            }).ToList();

            return View(cartModel);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var orderData = await SalesDataService.GetOrderAsync(id);

            // Kiểm tra tồn tại và trạng thái (Chỉ cho phép sửa khi Status là New)
            if (orderData == null || orderData.Status != OrderStatusEnum.New)
                return RedirectToAction("Detail", new { id = id });
  var model = new OrderItemModel
            {
                OrderID = orderData.OrderID,
                OrderTime = orderData.OrderTime,
                CustomerName = orderData.CustomerName ?? "",
                DeliveryAddress = orderData.DeliveryAddress ?? "",
                DeliveryProvince = orderData.DeliveryProvince ?? "",
                Status = (int)orderData.Status,
                TotalAmount = orderData.TotalAmount
            };

            ViewBag.ProvinceList = await SelectListHelper.Provinces();
            return View(model); // Trả về model đã được ép kiểu đúng
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int orderId, string deliveryName, string deliveryProvince, string deliveryAddress)
        {
            try
            {
                var orderInfo = await SalesDataService.GetOrderAsync(orderId);
                if (orderInfo != null && orderInfo.Status == OrderStatusEnum.New)
                {
                    
                    var orderToUpdate = new Order
                    {
                        OrderID = orderInfo.OrderID,
                        CustomerID = orderInfo.CustomerID,
                        DeliveryProvince = deliveryProvince,
                        DeliveryAddress = deliveryAddress,
                        Status = orderInfo.Status
                    };

                    await SalesDataService.UpdateOrderAsync(orderToUpdate);
                    TempData["Success"] = "Cập nhật thành công";
                }
                return RedirectToAction("Detail", new { id = orderId });
            }
            catch
            {
                TempData["Error"] = "Cập nhật thất bại";
                return RedirectToAction("Edit", new { id = orderId });
            }
        }


       
        [HttpPost]
        [Route("Order/Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // 1. Kiểm tra đơn hàng có tồn tại không
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return Json(new { success = false, message = "Đơn hàng không tồn tại." });

            // 2. Rào lại lần nữa: Chỉ cho hủy nếu trạng thái là "Vừa tạo" (Status = 1)
            if (order.Status != OrderStatusEnum.New)
                return Json(new { success = false, message = "Đơn hàng này đã được duyệt, không thể hủy." });

            // 3. Thực hiện cập nhật trạng thái trong DB thành "Đã hủy"
            bool result = await SalesDataService.CancelOrderAsync(id);

            if (result)
                return Json(new { success = true, message = "Đã hủy đơn hàng thành công!" });

            return Json(new { success = false, message = "Lỗi hệ thống khi hủy đơn hàng." });
        }
    }
}