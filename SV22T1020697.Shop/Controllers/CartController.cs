using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SV22T1020697.Shop;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Sales;
using System.Data;

namespace SV22T102097.Shop.Controllers
{
    public class CartController : Controller
    {
        private readonly string _connectionString;

        public CartController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LiteCommerceDB")
                                ?? throw new Exception("Chưa cấu hình chuỗi kết nối!");
        }

        public IActionResult Index()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Khôi phục giỏ hàng từ Database vào Session khi khách hàng đăng nhập
        /// </summary>
        public async Task<IActionResult> RestoreCart()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (customerId.HasValue)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"SELECT c.ProductID, p.ProductName, p.Photo, p.Unit, c.Quantity, p.Price as SalePrice
                                   FROM Cart c 
                                   JOIN Products p ON c.ProductID = p.ProductID 
                                   WHERE c.CustomerID = @id";

                    var dbItems = await connection.QueryAsync<CartItem>(sql, new { id = customerId });

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
                        await SyncCartToDbAsync();
                    }
                }
            }
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Đồng bộ dữ liệu từ Session xuống Database Table Cart
        /// </summary>
        private async Task SyncCartToDbAsync()
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue) return;

            var cart = ShoppingCartHelper.GetShoppingCart();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Xóa sạch giỏ hàng cũ của User trong DB
                        await connection.ExecuteAsync("DELETE FROM Cart WHERE CustomerID = @id", new { id = customerId }, transaction);

                        // Nếu giỏ hàng hiện tại (Session) có hàng thì mới Insert lại
                        if (cart != null && cart.Any())
                        {
                            string sqlInsert = "INSERT INTO Cart(CustomerID, ProductID, Quantity, AddedDate) VALUES(@CustomerID, @ProductID, @Quantity, GETDATE())";
                            var parameters = cart.Select(item => new {
                                CustomerID = customerId,
                                ProductID = item.ProductID,
                                Quantity = item.Quantity
                            });
                            await connection.ExecuteAsync(sqlInsert, parameters, transaction);
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> Add(int id, int quantity = 1, bool redirect = false)
        {
            if (quantity <= 0) return Json(new { code = 0, message = "Số lượng không hợp lệ" });

            using (var connection = new SqlConnection(_connectionString))
            {
                var product = await connection.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE ProductID = @id", new { id });
                if (product == null) return Json(new { code = 0, message = "Sản phẩm không tồn tại" });

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
                await SyncCartToDbAsync();

                var currentCart = ShoppingCartHelper.GetShoppingCart();
                return Json(new { code = 1, message = "Đã thêm vào giỏ hàng", data = currentCart.Count });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, int quantity)
        {
            if (quantity <= 0) ShoppingCartHelper.RemoveItemFromCart(id);
            else ShoppingCartHelper.UpdateCartItem(id, quantity);

            await SyncCartToDbAsync();

            var cart = ShoppingCartHelper.GetShoppingCart();
            return Json(new { code = 1, cartCount = cart.Count, cartTotal = cart.Sum(m => m.TotalPrice) });
        }

        public async Task<IActionResult> Remove(int id)
        {
            ShoppingCartHelper.RemoveItemFromCart(id);
            await SyncCartToDbAsync();
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa sạch giỏ hàng (Gọi sau khi đặt hàng thành công)
        /// </summary>
        public async Task<IActionResult> Clear()
        {
            ShoppingCartHelper.ClearCart(); // Xóa trên Session
            await SyncCartToDbAsync();      // Xóa trong Database (vì session đã trống nên hàm Sync sẽ DELETE DB)
            return RedirectToAction("Index");
        }
    }
}