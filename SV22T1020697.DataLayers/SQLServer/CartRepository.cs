using Dapper;
using Microsoft.Data.SqlClient; // Thêm cái này để nhận diện SqlConnection nếu cần
using SV22T1020697.DataLayers.Interfaces; // Khớp với folder Interfaces
using SV22T1020697.DataLayers.SqlServer;
using SV22T1020697.Models.Sales;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class CartRepository : BaseRepository, ICartRepository
    {
        // Constructor kế thừa từ BaseRepository để lấy ConnectionString
        public CartRepository(string connectionString) : base(connectionString) { }

        public IEnumerable<CartItem> GetCartItems(int customerId)
        {
            using (var connection = OpenConnection()) // Hàm này nằm trong BaseRepository
            {
                string sql = @"SELECT c.ProductID, p.ProductName, p.Photo, p.Unit, c.Quantity, p.Price as SalePrice
                               FROM Cart c 
                               JOIN Products p ON c.ProductID = p.ProductID 
                               WHERE c.CustomerID = @id";
                return connection.Query<CartItem>(sql, new { id = customerId });
            }
        }

        public bool SyncCart(int customerId, IEnumerable<CartItem> cartItems)
        {
            using (var connection = OpenConnection())
            {
                // THÊM DÒNG NÀY ĐỂ MỞ KẾT NỐI TRƯỚC KHI DÙNG TRANSACTION
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Xóa sạch giỏ hàng cũ của khách trong DB
                        connection.Execute("DELETE FROM Cart WHERE CustomerID = @id", new { id = customerId }, transaction);

                        // 2. Nếu giỏ hàng hiện tại có hàng thì mới Insert lại
                        if (cartItems != null && cartItems.Any())
                        {
                            string sqlInsert = @"INSERT INTO Cart(CustomerID, ProductID, Quantity, AddedDate) 
                                         VALUES(@CustomerID, @ProductID, @Quantity, GETDATE())";

                            var parameters = cartItems.Select(item => new {
                                CustomerID = customerId,
                                ProductID = item.ProductID,
                                Quantity = item.Quantity
                            });

                            connection.Execute(sqlInsert, parameters, transaction);
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}