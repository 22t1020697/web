using Dapper;
using SV22T1020697.Datalayers.SqlServer;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;

namespace SV22T1020697.Datalayers.SqlServer
{
    /// <summary>
    /// Lớp thực hiện truy xuất dữ liệu bảng Categories trong SQL Server
    /// sử dụng thư viện Dapper
    /// </summary>
    public class CategoryRepository :
        BaseRepository,
        IGenericRepository<Category>,
        IDataDictionaryRepository<Category>
    {
        /// <summary>
        /// Constructor nhận chuỗi kết nối database
        /// </summary>
        public CategoryRepository(string connectionString)
            : base(connectionString)
        {
        }

        /// <summary>
        /// Thêm loại hàng mới
        /// </summary>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"INSERT INTO Categories
                           (CategoryName, Description)
                           VALUES
                           (@CategoryName, @Description);
                           SELECT CAST(SCOPE_IDENTITY() AS INT)";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật loại hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = OpenConnection();

            string sql = @"UPDATE Categories
                           SET CategoryName = @CategoryName,
                               Description = @Description
                           WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM Categories
                           WHERE CategoryID = @id";

            int rows = await connection.ExecuteAsync(sql, new { id });
            return rows > 0;
        }

        /// <summary>
        /// Lấy thông tin một loại hàng
        /// </summary>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM Categories
                           WHERE CategoryID = @id";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { id });
        }

       

        /// <summary>
        /// Lấy danh sách loại hàng có phân trang và tìm kiếm
        /// </summary>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "";
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
                condition = "WHERE CategoryName LIKE @SearchValue";

            // 1. Đếm tổng số dòng
            string countSql = $"SELECT COUNT(*) FROM Categories {condition}";
            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" });

            if (result.RowCount == 0)
                return result;

            // 2. Xử lý câu lệnh SQL linh hoạt
            string querySql;
            if (input.PageSize > 0)
            {
                // Có phân trang
                querySql = $@"SELECT *
                      FROM Categories
                      {condition}
                      ORDER BY CategoryName
                      OFFSET @Offset ROWS
                      FETCH NEXT @PageSize ROWS ONLY";
            }
            else
            {
                // Không phân trang
                querySql = $@"SELECT *
                      FROM Categories
                      {condition}
                      ORDER BY CategoryName";
            }

            result.DataItems = (await connection.QueryAsync<Category>(querySql,
                new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                })).ToList();

            return result;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách category (dùng cho dropdown)
        /// </summary>
        public async Task<List<Category>> ListAsync()
        {
            using var connection = OpenConnection();

            string sql = @"SELECT *
                           FROM Categories
                           ORDER BY CategoryName";

            var data = await connection.QueryAsync<Category>(sql);

            return data.ToList();
        }
        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng hay không
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> IsUsed(int id)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE CategoryID = @id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            return await IsUsed(id);
        }
    }
}
