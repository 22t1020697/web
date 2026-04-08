using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.Catalog;
using SV22T1020697.Models.Common;
using System.Data;

namespace SV22T1020697.DataLayers.SQLServer
{
    /// <summary>
    /// Truy xuất dữ liệu cho Products, ProductAttributes, ProductPhotos
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        // ================= PRODUCT =================

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = OpenConnection();

            var result = new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string condition = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
                condition += " AND ProductName LIKE @SearchValue";

            if (input.CategoryID > 0)
                condition += " AND CategoryID = @CategoryID";

            if (input.SupplierID > 0)
                condition += " AND SupplierID = @SupplierID";

            if (input.MinPrice > 0)
                condition += " AND Price >= @MinPrice";

            if (input.MaxPrice > 0)
                condition += " AND Price <= @MaxPrice";

            string countSql = $@"
            SELECT COUNT(*)
            FROM Products
            {condition}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    input.CategoryID,
                    input.SupplierID,
                    input.MinPrice,
                    input.MaxPrice
                });

            if (result.RowCount == 0)
                return result;

            string querySql = $@"
            SELECT *
            FROM Products
            {condition}
            ORDER BY ProductName
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

            result.DataItems = (await connection.QueryAsync<Product>(querySql, new
            {
                SearchValue = $"%{input.SearchValue}%",
                input.CategoryID,
                input.SupplierID,
                input.MinPrice,
                input.MaxPrice,
                Offset = input.Offset,
                PageSize = input.PageSize
            })).ToList();

            return result;
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * FROM Products WHERE ProductID = @productID";

            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { productID });
        }

        public async Task<int> AddAsync(Product data)
        {
            using var connection = OpenConnection();

            string sql = @"
            INSERT INTO Products
            (ProductName,ProductDescription,SupplierID,CategoryID,Unit,Price,Photo,IsSelling)
            VALUES
            (@ProductName,@ProductDescription,@SupplierID,@CategoryID,@Unit,@Price,@Photo,@IsSelling);

            SELECT CAST(SCOPE_IDENTITY() AS INT)";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = OpenConnection();

            string sql = @"
            UPDATE Products
            SET
                ProductName = @ProductName,
                ProductDescription = @ProductDescription,
                SupplierID = @SupplierID,
                CategoryID = @CategoryID,
                Unit = @Unit,
                Price = @Price,
                Photo = @Photo,
                IsSelling = @IsSelling
            WHERE ProductID = @ProductID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = "DELETE FROM Products WHERE ProductID = @productID";

            int rows = await connection.ExecuteAsync(sql, new { productID });
            return rows > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT COUNT(*) FROM OrderDetails WHERE ProductID = @productID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { productID });

            return count > 0;
        }

        // ================= ATTRIBUTES =================

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * FROM ProductAttributes WHERE ProductID = @productID ORDER BY DisplayOrder";

            var data = await connection.QueryAsync<ProductAttribute>(sql, new { productID });

            return data.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * FROM ProductAttributes WHERE AttributeID = @attributeID";

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();

            string sql = @"
            INSERT INTO ProductAttributes
            (ProductID,AttributeName,AttributeValue,DisplayOrder)
            VALUES
            (@ProductID,@AttributeName,@AttributeValue,@DisplayOrder);

            SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = OpenConnection();

            string sql = @"
            UPDATE ProductAttributes
            SET
                AttributeName = @AttributeName,
                AttributeValue = @AttributeValue,
                DisplayOrder = @DisplayOrder
            WHERE AttributeID = @AttributeID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM ProductAttributes WHERE AttributeID = @attributeID";

            int rows = await connection.ExecuteAsync(sql, new { attributeID });

            return rows > 0;
        }

        // ================= PHOTOS =================

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * FROM ProductPhotos WHERE ProductID = @productID ORDER BY DisplayOrder";

            var data = await connection.QueryAsync<ProductPhoto>(sql, new { productID });

            return data.ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = OpenConnection();

            string sql = @"SELECT * FROM ProductPhotos WHERE PhotoID = @photoID";

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();

            string sql = @"
            INSERT INTO ProductPhotos
            (ProductID,Photo,Description,DisplayOrder,IsHidden)
            VALUES
            (@ProductID,@Photo,@Description,@DisplayOrder,@IsHidden);

            SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = OpenConnection();

            string sql = @"
            UPDATE ProductPhotos
            SET
                Photo = @Photo,
                Description = @Description,
                DisplayOrder = @DisplayOrder,
                IsHidden = @IsHidden
            WHERE PhotoID = @PhotoID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = OpenConnection();

            string sql = @"DELETE FROM ProductPhotos WHERE PhotoID = @photoID";

            int rows = await connection.ExecuteAsync(sql, new { photoID });

            return rows > 0;
        }

        public Task<List<ProductPhoto>> ListPhotoAsync(int productID)
        {
            throw new NotImplementedException();
        }
        public Product? Get(int id)
        {
            using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
            {
                string sql = "SELECT * FROM Products WHERE ProductID = @id";
                return connection.QueryFirstOrDefault<Product>(sql, new { id });
            }
        }
    }

}
