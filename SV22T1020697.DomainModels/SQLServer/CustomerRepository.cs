using Dapper;

using SV22T1020697.Datalayers.SqlServer;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Partner;
using System.Data;

namespace SV22T1020697.Datalayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện truy xuất dữ liệu bảng Customers trong SQL Server
    /// sử dụng thư viện Dapper
    /// </summary>
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {

        /// <summary>
        /// Constructor nhận chuỗi kết nối database
        /// </summary>
        public CustomerRepository(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Thêm khách hàng mới
        /// </summary>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"INSERT INTO Customers
                          (CustomerName,ContactName,Province,Address,Phone,Email,IsLocked)
                           VALUES
                          (@CustomerName,@ContactName,@Province,@Address,@Phone,@Email,@IsLocked);
                           SELECT CAST(SCOPE_IDENTITY() AS INT)";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Customers
                           SET CustomerName=@CustomerName,
                               ContactName=@ContactName,
                               Province=@Province,
                               Address=@Address,
                               Phone=@Phone,
                               Email=@Email,
                               IsLocked=@IsLocked
                           WHERE CustomerID=@CustomerID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = "DELETE FROM Customers WHERE CustomerID=@id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo ID
        /// </summary>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = "SELECT * FROM Customers WHERE CustomerID=@id";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        /// <summary>
        /// Kiểm tra khách hàng có đang được sử dụng hay không
        /// </summary>
        /// 
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE CustomerID=@id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        /// <summary>
        /// Lấy danh sách khách hàng có phân trang và tìm kiếm
        /// </summary>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
                condition = @"WHERE CustomerName LIKE @SearchValue 
                              OR ContactName LIKE @SearchValue
                              OR Phone LIKE @SearchValue";

            string countSql = $"SELECT COUNT(*) FROM Customers {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" });

            if (result.RowCount == 0)
                return result;

            string querySql = $@"SELECT *
                                FROM Customers
                                {condition}
                                ORDER BY CustomerName
                                OFFSET @Offset ROWS
                                FETCH NEXT @PageSize ROWS ONLY";

            result.DataItems = (await connection.QueryAsync<Customer>(
                querySql,
                new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                })).ToList();

            return result;
        }

        /// <summary>
        /// Kiểm tra email có hợp lệ hay không (không trùng)
        /// </summary>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = OpenConnection();

            string sql;

            if (id == 0)
            {
                sql = "SELECT COUNT(*) FROM Customers WHERE Email=@email";
                int count = await connection.ExecuteScalarAsync<int>(sql, new { email });
                return count == 0;
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email=@email AND CustomerID<>@id";

                int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });
                return count == 0;
            }
        }

        public async Task<bool> IsUsedAsync(int id) { 
            return await IsUsed(id);
        }

        // =================== Password ===================

        /// <summary>
        /// Thay đổi mật khẩu khách hàng KHÔNG cần mật khẩu cũ
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int id, string hashedPassword)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Customers
                           SET Password = @hashedPassword
                           WHERE CustomerID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id, hashedPassword });
            return rows > 0;
        }

        /// <summary>
        /// Thay đổi mật khẩu có kiểm tra mật khẩu cũ
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int id, string oldHashedPassword, string newHashedPassword)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Customers
                           SET Password = @newHashedPassword
                           WHERE CustomerID = @id
                             AND Password = @oldHashedPassword";

            int rows = await connection.ExecuteAsync(sql, new { id, oldHashedPassword, newHashedPassword });
            return rows > 0;
        }
    }
}