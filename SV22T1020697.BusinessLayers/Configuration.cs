using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020697.BusinessLayers
{
    /// <summary>
    /// lớp lưu giữ những thong tin cấu hình sử dụng trong businessLayer
    /// </summary>
    public class Configuration
    {
        private static string _connectionString = "";
        /// <summary>
        /// khởi tạo các cấu hình cho businessLayer (hàm này được gọi truocứ khi chạy ứng dụng)   
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        /// <summary>
        /// lấy chuỗi tham số kết nối đến SQL Server được sử dụng trong các repository của businessLayer    
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
