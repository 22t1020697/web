using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Partner;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Suppliers trong SQL Server
    /// thông qua thư viện Dapper.
    /// Cài đặt interface IGenericRepository cho entity Supplier.
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>,
    IDataDictionaryRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối đến SQL Server
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối đến cơ sở dữ liệu
        /// </summary>
        /// <returns>Đối tượng SqlConnection</returns>
        private IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Thêm mới một nhà cung cấp vào CSDL
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp</param>
        /// <returns>Mã SupplierID vừa được tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa nhà cung cấp theo SupplierID
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql,
                new { SupplierID = id });

            return rows > 0;
        }
        /// <summary>
        /// Lấy thông tin một nhà cung cấp theo mã SupplierID
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Đối tượng Supplier hoặc null nếu không tồn tại</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql,
                new { SupplierID = id });
        }
        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*) FROM Products WHERE SupplierID = @SupplierID";

            int count = await connection.ExecuteScalarAsync<int>(sql,
                new { SupplierID = id });

            return count > 0;
        }

        /// <summary>
        /// Truy vấn danh sách nhà cung cấp có phân trang và tìm kiếm theo tên
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dạng PagedResult</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string search = $"%{input.SearchValue}%";

            string countSql = @"
                SELECT COUNT(*)
                FROM Suppliers
                WHERE SupplierName LIKE @SearchValue
                      OR ContactName LIKE @SearchValue";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { SearchValue = search });

            if (input.PageSize == 0)
            {
                string sql = @"
                    SELECT *
                    FROM Suppliers
                    WHERE SupplierName LIKE @SearchValue
                          OR ContactName LIKE @SearchValue
                    ORDER BY SupplierName";

                var data = await connection.QueryAsync<Supplier>(sql,
                    new { SearchValue = search });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"
                    SELECT *
                    FROM Suppliers
                    WHERE SupplierName LIKE @SearchValue
                          OR ContactName LIKE @SearchValue
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Supplier>(sql,
                    new
                    {
                        SearchValue = search,
                        Offset = input.Offset,
                        PageSize = input.PageSize
                    });

                result.DataItems = data.ToList();
            }

            return result;
        }

        public async Task<bool> IsUsedAsync(int supplierID)
        {
            return await IsUsed(supplierID);
        }

        public Task<List<Supplier>> ListAsync()
        {
            throw new NotImplementedException();
        }
    }
}