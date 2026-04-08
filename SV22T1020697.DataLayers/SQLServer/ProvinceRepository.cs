using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.Models.DataDictionary;

namespace SV22T1020697.DataLayers.SqlServer
{
    /// <summary>
    /// Truy xuất dữ liệu Provinces
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Lấy danh sách tỉnh thành
        /// </summary>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = OpenConnection();

            string sql = @"SELECT ProvinceName
                           FROM Provinces
                           ORDER BY ProvinceName";

            var data = await connection.QueryAsync<Province>(sql);

            return data.ToList();
        }
    }
}