using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.SqlServer;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.HR;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    public class EmployeeRepository : BaseRepository , IEmployeeRepository
    {

        public EmployeeRepository(string connectionString)
            : base(connectionString)
        {
        }


        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();
            var result = new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string search = $"%{input.SearchValue}%";

            string countSql = @"
                SELECT COUNT(*)
                FROM Employees
                WHERE (@SearchValue = '%%'
                       OR FullName LIKE @SearchValue
                       OR Phone LIKE @SearchValue
                       OR Email LIKE @SearchValue)";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new { SearchValue = search });

            if (input.PageSize == 0)
            {
                string sql = @"
                    SELECT * FROM Employees
                    WHERE (@SearchValue = '%%'
                           OR FullName LIKE @SearchValue
                           OR Phone LIKE @SearchValue
                           OR Email LIKE @SearchValue)
                    ORDER BY FullName";

                var data = await connection.QueryAsync<Employee>(sql, new { SearchValue = search });
                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"
                    SELECT * FROM Employees
                    WHERE (@SearchValue = '%%'
                           OR FullName LIKE @SearchValue
                           OR Phone LIKE @SearchValue
                           OR Email LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Employee>(sql, new
                {
                    SearchValue = search,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                });
                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = OpenConnection();
            string sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = OpenConnection();
            string sql = @"
                INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking, RoleNames)
                VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Password, @Photo, @IsWorking, @RoleNames);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = OpenConnection();
            string sql = @"
                UPDATE Employees
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Password = @Password,
                    Photo = @Photo,
                    IsWorking = @IsWorking,
                    RoleNames = @RoleNames
                WHERE EmployeeID = @EmployeeID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();
            string sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
            int rows = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return rows > 0;
        }

        

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();
            string sql;
            if (id == 0)
            {
                sql = @"SELECT COUNT(*) FROM Employees WHERE Email = @Email";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
                return count == 0;
            }
            else
            {
                sql = @"SELECT COUNT(*) FROM Employees WHERE Email = @Email AND EmployeeID <> @EmployeeID";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, EmployeeID = id });
                return count == 0;
            }
        }
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();
            string sql = @"SELECT COUNT(*) FROM Orders WHERE EmployeeID = @EmployeeID";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeID = id });
            return count > 0;
        }
        public async Task<bool> IsUsedAsync(int id)
        {
            return await IsUsed(id);
        }
        // File: EmployeeAccountRepository.cs
        public class EmployeeAccountRepository
        {
            private readonly string _connectionString;

            public EmployeeAccountRepository(string connectionString)
            {
                // Dòng này giúp xóa bỏ cảnh báo "Possible null reference"
                _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            }
        }
    }
}