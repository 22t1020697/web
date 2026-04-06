using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Common;
using SV22T1020697.Models.Partner;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Shippers trong SQL Server
    /// thông qua thư viện Dapper.
    /// Cài đặt interface IGenericRepository cho entity Shipper.
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>,
        IDataDictionaryRepository<Shipper>

    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối đến SQL Server
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public ShipperRepository(string connectionString)
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
        /// Truy vấn danh sách người giao hàng có phân trang và tìm kiếm theo tên
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dạng PagedResult</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string search = $"%{input.SearchValue}%";

            string countSql = @"
                SELECT COUNT(*)
                FROM Shippers
                WHERE (@SearchValue = '%%'
                       OR ShipperName LIKE @SearchValue
                       OR Phone LIKE @SearchValue)";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { SearchValue = search });

            if (input.PageSize == 0)
            {
                string sql = @"
                    SELECT *
                    FROM Shippers
                    WHERE (@SearchValue = '%%'
                           OR ShipperName LIKE @SearchValue
                           OR Phone LIKE @SearchValue)
                    ORDER BY ShipperName";

                var data = await connection.QueryAsync<Shipper>(sql,
                    new { SearchValue = search });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = @"
                    SELECT *
                    FROM Shippers
                    WHERE (@SearchValue = '%%'
                           OR ShipperName LIKE @SearchValue
                           OR Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Shipper>(sql,
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

        /// <summary>
        /// Lấy thông tin một người giao hàng theo mã ShipperID
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Đối tượng Shipper hoặc null nếu không tồn tại</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(sql,
                new { ShipperID = id });
        }

        /// <summary>
        /// Thêm mới một người giao hàng vào CSDL
        /// </summary>
        /// <param name="data">Thông tin người giao hàng</param>
        /// <returns>Mã ShipperID vừa được tạo</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Shippers(ShipperName, Phone)
                VALUES(@ShipperName, @Phone);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Shippers
                SET ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa người giao hàng theo ShipperID
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql,
                new { ShipperID = id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*) FROM Orders WHERE ShipperID = @ShipperID";

            int count = await connection.ExecuteScalarAsync<int>(sql,
                new { ShipperID = id });

            return count > 0;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách shipper (dùng cho dropdown)
        /// </summary>
        public async Task<List<Shipper>> ListAsync()
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                   FROM Shippers
                   ORDER BY ShipperName";

            var data = await connection.QueryAsync<Shipper>(sql);

            return data.ToList();
        }
        /// <summary>
        /// Mở kết nối SQL Server
        /// </summary>
        private IDbConnection OpenConnection()
        {

            return new SqlConnection(_connectionString);
        }
        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*) FROM Orders WHERE ShipperID = @ShipperID";

            int count = await connection.ExecuteScalarAsync<int>(sql,
                new { ShipperID = id });

            return count > 0;
        }

    }
}