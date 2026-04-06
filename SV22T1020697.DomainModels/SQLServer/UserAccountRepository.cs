using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Security;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class UserAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public UserAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = OpenConnection();

            string sql = @"
                SELECT EmployeeID AS UserID, 
                       Email AS UserName, 
                       FullName AS DisplayName, 
                       Photo, 
                       RoleNames 
                FROM Employees 
                WHERE Email = @Email AND Password = @Password AND IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                Email = userName,
                Password = password
            });
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = OpenConnection();

            string sql = @"
                UPDATE Employees
                SET Password = @Password
                WHERE Email = @Email";

            int rows = await connection.ExecuteAsync(sql, new
            {
                Email = userName,
                Password = password
            });
            return rows > 0;
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            // Giả sử logic kiểm tra DB của bạn ở đây
            // Nếu chưa viết xong, hãy trả về Task.FromResult<UserAccount?>(null)
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT UserID, UserName, DisplayName, Photo, RoleNames 
                       FROM Employees 
                       WHERE Email = @Email AND Password = @Password";

                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
                {
                    Email = userName,
                    Password = password
                });
            }
        }
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            // Thay vì throw null, hãy dùng exception cụ thể hoặc viết logic thực tế
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE Employees SET Password = @Password WHERE Email = @Email";
                int rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    Email = userName,
                    Password = password
                });
                return rowsAffected > 0;
            }
        }
    }
}