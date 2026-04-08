using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.DataLayers.SqlServer;
using SV22T1020697.DomainModels;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Sales;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class OrderRepository : BaseRepository, IOrderRepository
    {
       

        public OrderRepository(string connectionString): base(connectionString)
        {
          
        }


        // ================= LIST ORDER =================
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT o.*,
                      c.CustomerName,
                      c.ContactName CustomerContactName,
                      c.Email CustomerEmail,
                      c.Phone CustomerPhone,
                      c.Address CustomerAddress,
                      e.FullName AS EmployeeName,
                      s.ShipperName,
                      s.Phone ShipperPhone,
                      (SELECT SUM(Quantity * SalePrice) 
                       FROM OrderDetails 
                       WHERE OrderID = o.OrderID) as TotalAmount 
               FROM Orders o
               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
               LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
               LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
               WHERE (@Status = 0 OR o.Status = @Status)
               AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
               AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
               -- Thứ tự bắt buộc: ORDER BY xong mới đến OFFSET
               ORDER BY o.OrderTime DESC
               OFFSET @Offset ROWS
               FETCH NEXT @PageSize ROWS ONLY";

            var data = await connection.QueryAsync<OrderViewInfo>(sql, new
            {
                input.Status,
                input.DateFrom,
                input.DateTo,
                Offset = input.Offset,
                input.PageSize
            });

            string countSql = @"SELECT COUNT(*)
                                FROM Orders
                                WHERE (@Status = 0 OR Status = @Status)
                                AND (@DateFrom IS NULL OR OrderTime >= @DateFrom)
                                AND (@DateTo IS NULL OR OrderTime <= @DateTo)";

            int count = await connection.ExecuteScalarAsync<int>(countSql, input);

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        // ================= GET ORDER =================
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT o.*,
                      c.CustomerName,
                      c.ContactName CustomerContactName,
                      c.Email CustomerEmail,
                      c.Phone CustomerPhone,
                      c.Address CustomerAddress,
                      e.FullName AS EmployeeName,
                      s.ShipperName,
                      s.Phone ShipperPhone, 
                      (SELECT SUM(Quantity * SalePrice) 
                       FROM OrderDetails 
                       WHERE OrderID = o.OrderID) as TotalAmount
               FROM Orders o
               LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
               LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
               LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
               WHERE o.OrderID = @orderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        // ================= ADD ORDER =================
        public async Task<int> AddAsync(Order data)
        {
            // Dùng cú pháp using mới cho gọn
            using var connection = OpenConnection();

            // Dùng OUTPUT INSERTED cho an toàn ở phía SQL
            string sql = @"INSERT INTO Orders(OrderTime, CustomerID, DeliveryProvince, DeliveryAddress, Status) 
                   OUTPUT INSERTED.OrderID
                   VALUES(@OrderTime, @CustomerID, @DeliveryProvince, @DeliveryAddress, @Status)";

            // Truyền thẳng data vào Dapper cho ngắn gọn, không cần tạo object ẩn danh
            return await connection.QuerySingleAsync<int>(sql, data);
        }
        // ================= UPDATE ORDER =================
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Orders
                           SET CustomerID = @CustomerID,
                               DeliveryProvince = @DeliveryProvince,
                               DeliveryAddress = @DeliveryAddress,
                               EmployeeID = @EmployeeID,
                               AcceptTime = @AcceptTime,
                               ShipperID = @ShipperID,
                               ShippedTime = @ShippedTime,
                               FinishedTime = @FinishedTime,
                               Status = @Status
                           WHERE OrderID = @OrderID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        // ================= DELETE ORDER =================
        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = OpenConnection();

            await connection.ExecuteAsync(
                "DELETE FROM OrderDetails WHERE OrderID=@orderID",
                new { orderID });

            int rows = await connection.ExecuteAsync(
                "DELETE FROM Orders WHERE OrderID=@orderID",
                new { orderID });

            return rows > 0;
        }

        // ================= LIST ORDER DETAILS =================
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT d.*,
                                  p.ProductName,
                                  p.Unit,
                                  p.Photo
                           FROM OrderDetails d
                           JOIN Products p ON d.ProductID = p.ProductID
                           WHERE d.OrderID = @orderID";

            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });
            return data.ToList();
        }

        // ================= GET DETAIL =================
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT d.*,
                                  p.ProductName,
                                  p.Unit,
                                  p.Photo
                           FROM OrderDetails d
                           JOIN Products p ON d.ProductID = p.ProductID
                           WHERE d.OrderID = @orderID AND d.ProductID = @productID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                sql, new { orderID, productID });
        }

        // ================= ADD DETAIL =================
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();

            string sql = @"INSERT INTO OrderDetails
                           (OrderID, ProductID, Quantity, SalePrice)
                           VALUES
                           (@OrderID, @ProductID, @Quantity, @SalePrice)";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        // ================= UPDATE DETAIL =================
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE OrderDetails
                           SET Quantity = @Quantity,
                               SalePrice = @SalePrice
                           WHERE OrderID = @OrderID AND ProductID = @ProductID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        // ================= DELETE DETAIL =================
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM OrderDetails
                           WHERE OrderID = @orderID
                           AND ProductID = @productID";

            int rows = await connection.ExecuteAsync(sql, new { orderID, productID });
            return rows > 0;
        }

        
    }
}