using Dapper;
using SV22T1020697.Datalayers.SqlServer;
using SV22T1020697.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class CustomerAccountRepository : BaseRepository
    {
        public CustomerAccountRepository(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        public async Task<Customer?> LoginAsync(string email, string password)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM Customers
                           WHERE Email = @email
                           AND Password = @password
                           AND IsLocked = 0";

            return await connection.QueryFirstOrDefaultAsync<Customer>(
                sql,
                new { email, password });
        }

        /// <summary>
        /// Thêm mới khách hàng (Đăng ký tài khoản)
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"INSERT INTO Customers(CustomerName, ContactName, Email, Phone, Password, IsLocked)
                           OUTPUT INSERTED.CustomerID
                           VALUES(@CustomerName, @ContactName, @Email, @Phone, @Password, @IsLocked)";

            // Sử dụng ExecuteScalarAsync để lấy ID vừa được tạo tự động trong DB
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int customerID, string password)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Customers
                           SET Password = @password
                           WHERE CustomerID = @customerID";

            int rows = await connection.ExecuteAsync(
                sql,
                new { customerID, password });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra email đã tồn tại hay chưa (dùng cho Đăng ký hoặc Đổi email)
        /// </summary>
        public async Task<bool> ExistsAsync(string email)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Customers
                           WHERE Email = @email";

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { email });

            return count > 0;
        }

        /// <summary>
        /// Lấy thông tin khách hàng theo ID
        /// </summary>
        public async Task<Customer?> GetAsync(int customerID)
        {
            using var connection = OpenConnection();
            // Đảm bảo lấy tất cả các cột
            string sql = @"SELECT * FROM Customers WHERE CustomerID = @customerID";
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { customerID });
        }
    }
}