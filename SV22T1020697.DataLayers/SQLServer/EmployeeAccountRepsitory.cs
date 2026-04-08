using Dapper;
using SV22T1020697.DataLayers.SqlServer;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Security;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : BaseRepository, IUserAccountRepository
    {
        public EmployeeAccountRepository(string connectionString)
            : base(connectionString)
        {
        }
        private string HashMD5(string input)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return string.Concat(hashBytes.Select(b => b.ToString("x2")));
        }

        /// kiểm tra đăng nhập
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = OpenConnection();

          

            string sql = @"SELECT 
                                EmployeeID AS UserId,
                                Email AS UserName,
                                FullName AS DisplayName,
                                Photo,
                                RoleNames
                           FROM Employees
                           WHERE Email = @userName
                           AND Password = @password
                           AND IsWorking = 1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new { userName, password  });
        }

        /// đổi mật khẩu(có mật khẩu cũ)
        public async Task<bool> ChangePasswordAsync(string userName, string oldPassword, string newPassword)
        {
            using IDbConnection connection = OpenConnection();
    

            string sql = @"UPDATE Employees
                           SET Password = @newpassword
                           WHERE Email = @userName AND Password = @oldPassword";
            int rows = await connection.ExecuteAsync(sql,
                            new {
                                userName = userName,
                                oldPassword = oldPassword,
                                newPassword = newPassword
                            });

            return rows > 0;
        }
        //đổi mật khẩu ( ko cần mk cũ)
        public async Task<bool> ChangePasswordAsync(string userName, string newPassword)
        {
            using var connection = OpenConnection();

            

            string sql = @"UPDATE Employees
                           SET Password = @newPassword
                           WHERE Email = @userName";

            int rows = await connection.ExecuteAsync(sql,
                new { userName, newPassword  });

            return rows > 0;
        }
    }
}
