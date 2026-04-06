using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Sales;
using SV22T1020697.Shop; // Giả sử ShoppingCartHelper nằm trong namespace này

namespace SV22T1020697.Shop.Controllers
{
    public class OrderController : Controller
    {
        private readonly string _connectionString;

        public OrderController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LiteCommerceDB")
                                ?? throw new Exception("Chưa cấu hình chuỗi kết nối LiteCommerceDB!");
        }

        /// <summary>
        /// Hiển thị trang xác nhận đơn hàng
        /// </summary>
        public async Task<IActionResult> Index(string selectedIds, int? productId, int quantity = 1)
        {
            var model = new List<CartItem>();

            // 1. TRƯỜNG HỢP MUA NGAY (Từ nút Mua ngay ở trang chi tiết sản phẩm)
            if (productId.HasValue)
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var product = await connection.QueryFirstOrDefaultAsync<Product>(
                        "SELECT * FROM Products WHERE ProductID = @id",
                        new { id = productId });

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
            }
            // 2. TRƯỜNG HỢP TỪ GIỎ HÀNG (Lọc các sản phẩm được chọn)
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sqlCustomer = "SELECT * FROM Customers WHERE CustomerID = @id";
                    var customer = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlCustomer, new { id = customerId });
                    ViewBag.Customer = customer;
                }
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

            // Nếu đặt hàng từ giỏ nhưng không tìm thấy item trong session (hết hạn session)
            if (checkoutItems.Count == 0 && !selectedIds.Contains(","))
            {
                // Đoạn này xử lý dự phòng cho trường hợp Mua Ngay không qua giỏ hàng
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

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Lưu đơn hàng
                            string sqlOrder = @"INSERT INTO Orders(OrderTime, CustomerID, DeliveryProvince, DeliveryAddress, Status) 
                                               OUTPUT INSERTED.OrderID
                                               VALUES(GETDATE(), @CustomerID, @Province, @Address, 1)";

                            int orderId = await connection.QuerySingleAsync<int>(sqlOrder, new
                            {
                                CustomerID = customerId,
                                Province = deliveryProvince,
                                Address = deliveryAddress
                            }, transaction);

                            // 2. Lưu chi tiết đơn hàng
                            string sqlDetail = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice) VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                            foreach (var item in checkoutItems)
                            {
                                await connection.ExecuteAsync(sqlDetail, new
                                {
                                    OrderID = orderId,
                                    ProductID = item.ProductID,
                                    Quantity = item.Quantity,
                                    SalePrice = item.SalePrice
                                }, transaction);
                            }

                            // 3. XÓA TRONG DATABASE (Bảng Cart tạm lưu trữ)
                            await connection.ExecuteAsync("DELETE FROM Cart WHERE CustomerID = @cid AND ProductID IN @pids",
                                                          new { cid = customerId, pids = ids }, transaction);

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                // 4. CẬP NHẬT LẠI SESSION GIỎ HÀNG (Dọn dẹp bộ nhớ tạm)
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

            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT OrderID, OrderTime, Status, DeliveryAddress, DeliveryProvince,
                            (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = Orders.OrderID) as TotalAmount
                             FROM Orders WHERE CustomerID = @customerId ORDER BY OrderTime DESC";
                var orders = await connection.QueryAsync<OrderItemModel>(sql, new { customerId });
                return View(orders);
            }
        }

        public async Task<IActionResult> Detail(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var order = await connection.QueryFirstOrDefaultAsync<OrderItemModel>(
                    @"SELECT o.*, c.CustomerName,
                             (SELECT SUM(Quantity * SalePrice) FROM OrderDetails WHERE OrderID = o.OrderID) as TotalAmount 
                      FROM Orders o
                      LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                      WHERE o.OrderID = @id", new { id });

                if (order is null) return RedirectToAction("History");

                var detail = await connection.QueryAsync<CartItem>(
                    @"SELECT d.*, p.ProductName, p.Photo, p.Unit 
                      FROM OrderDetails d JOIN Products p ON d.ProductID = p.ProductID 
                      WHERE d.OrderID = @id", new { id });

                ViewBag.Order = order;
                return View(detail);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var order = await connection.QueryFirstOrDefaultAsync<OrderItemModel>(
                    @"SELECT o.*, c.CustomerName FROM Orders o 
                      LEFT JOIN Customers c ON o.CustomerID = c.CustomerID 
                      WHERE o.OrderID = @id", new { id });

                if (order == null || order.Status != 1)
                    return RedirectToAction("Detail", new { id = id });

                ViewBag.ProvinceList = await SelectListHelper.Provinces();
                return View(order);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int orderId, string deliveryName, string deliveryProvince, string deliveryAddress)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    string sql = @"UPDATE Orders SET DeliveryProvince = @province, DeliveryAddress = @address 
                                   WHERE OrderID = @id AND Status = 1";
                    await connection.ExecuteAsync(sql, new { province = deliveryProvince, address = deliveryAddress, id = orderId });
                }
                TempData["Success"] = "Cập nhật thành công";
                return RedirectToAction("Detail", new { id = orderId });
            }
            catch
            {
                TempData["Error"] = "Cập nhật thất bại";
                return RedirectToAction("Edit", new { id = orderId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int? customerId = HttpContext.Session.GetInt32("CustomerID");
            if (!customerId.HasValue) return Json(new { success = false, message = "Chưa đăng nhập!" });

            using (var connection = new SqlConnection(_connectionString))
            {
                var order = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Status FROM Orders WHERE OrderID = @id AND CustomerID = @customerId",
                    new { id, customerId });

                if (order is null) return Json(new { success = false, message = "Đơn không tồn tại!" });
                if ((int)order.Status != 1) return Json(new { success = false, message = "Chỉ huỷ được đơn mới tạo!" });

                await connection.ExecuteAsync("UPDATE Orders SET Status = -1 WHERE OrderID = @id", new { id });
                return Json(new { success = true, message = "Huỷ đơn thành công!" });
            }
        }
    }
}