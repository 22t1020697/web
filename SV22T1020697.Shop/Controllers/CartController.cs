using Microsoft.AspNetCore.Mvc;
using SV22T1020697.BusinessLayers;
using SV22T1020697.Shop;
using SV22T1020697.Models.Sales;

namespace SV22T1020697.Shop.Controllers
{
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Khôi phục giỏ hàng từ Database vào Session khi khách hàng đăng nhập
        /// </summary>
        public IActionResult RestoreCart()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId.HasValue)
            {
                // Gọi Service để lấy danh sách từ DB (Không viết SQL ở đây)
                var dbItems = SalesDataService.ListCartItems(customerId.Value);

                if (dbItems != null)
                {
                    var sessionCart = ShoppingCartHelper.GetShoppingCart();
                    foreach (var item in dbItems)
                    {
                        var existItem = sessionCart.FirstOrDefault(m => m.ProductID == item.ProductID);
                        if (existItem == null)
                        {
                            ShoppingCartHelper.AddItemToCart(item);
                        }
                        else
                        {
                            existItem.Quantity += item.Quantity;
                            ShoppingCartHelper.UpdateCartItem(existItem.ProductID, existItem.Quantity);
                        }
                    }
                    SyncCartToDb();
                }
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đồng bộ dữ liệu từ Session xuống Database Table Cart thông qua Service
        /// </summary>
        private void SyncCartToDb()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue) return;

            var cart = ShoppingCartHelper.GetShoppingCart();

            // Gọi Business Layer xử lý việc lưu trữ
            SalesDataService.SyncCart(customerId.Value, cart);
        }

        

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Add(int id, int quantity = 1, bool redirect = false)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new { code = 0, message = "Số lượng không hợp lệ" });

                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                    return Json(new { code = 0, message = "Sản phẩm không tồn tại" });

                if (redirect)
                {
                    return RedirectToAction("Index", "Order", new { productId = id, quantity = quantity });
                }

                var item = new CartItem()
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName ?? "Sản phẩm",
                    Photo = product.Photo ?? "nophoto.png",
                    Unit = product.Unit ?? "",
                    Quantity = quantity,
                    SalePrice = product.Price
                };

                ShoppingCartHelper.AddItemToCart(item);
                SyncCartToDb(); // Lỗi rất có thể đang nổ ở hàm Sync này!

                var currentCart = ShoppingCartHelper.GetShoppingCart();
                return Json(new { code = 1, message = "Đã thêm vào giỏ hàng", data = currentCart.Count });
            }
            catch (Exception ex)
            {
                // Bắt tận tay lỗi và quăng ra màn hình
                return Json(new { code = 0, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            if (quantity <= 0)
                ShoppingCartHelper.RemoveItemFromCart(id);
            else
                ShoppingCartHelper.UpdateCartItem(id, quantity);

            SyncCartToDb();

            var cart = ShoppingCartHelper.GetShoppingCart();
            return Json(new { code = 1, cartCount = cart.Count, cartTotal = cart.Sum(m => m.TotalPrice) });
        }

        public IActionResult Remove(int id)
        {
            ShoppingCartHelper.RemoveItemFromCart(id);
            SyncCartToDb();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        public IActionResult Clear()
        {
            ShoppingCartHelper.ClearCart();
            SyncCartToDb();
            return RedirectToAction("Index");
        }
    }
}