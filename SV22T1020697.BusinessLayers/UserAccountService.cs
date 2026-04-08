using SV22T1020697.DataLayers;
using SV22T1020697.DataLayers.Interfaces;
using SV22T1020697.DataLayers.SQLServer;
using SV22T1020697.Models.Partner;
using SV22T1020697.Models.Security;
using System.Threading.Tasks;
namespace SV22T1020697.BusinessLayers
{
    /// <summary>
    /// Cung cấp các dịch vụ liên quan đến tài khoản người dùng.
    /// Lưu ý: Tầng này không trực tiếp sử dụng CryptHelper của tầng Web (Shop) 
    /// để tránh lỗi tham chiếu vòng (Circular Dependency).
    /// </summary>
    public static class UserAccountService
    {
        private static readonly ICustomerAccountRepository customerAccountDB;
        private static readonly IUserAccountRepository employeeDB;
        static UserAccountService()
        {
            string connectionString = Configuration.ConnectionString;
            customerAccountDB = new CustomerAccountRepository(connectionString);
            employeeDB = new EmployeeAccountRepository(connectionString);
        }
        /// <summary>
    /// Xác thực cho trang ADMIN (Chấp nhận cả Admin và Employee)
    /// </summary>
    public static async Task<UserAccount?> AuthorizeAsync(string username, string hashedPassword)
    {
        // Chỉ quét bảng Employees vì trang Admin không cho Khách hàng vào
        return await employeeDB.AuthorizeAsync(username, hashedPassword);
    }
        /// <summary>
        /// Xác thực đăng nhập. 
        /// Tham số hashedPassword phải là mật khẩu đã được mã hóa từ Controller.
        /// </summary>
        public static async Task<Customer?> AuthorizeCustomerAsync(string email, string hashedPassword)
        {
            // Tầng Business chỉ điều phối, không chứa SQL, không chứa logic mã hóa của Web
            return await customerAccountDB.LoginAsync(email, hashedPassword);
        }

        /// <summary>
        /// Đăng ký tài khoản mới.
        /// Thuộc tính Password trong đối tượng data phải được mã hóa sẵn trước khi gọi hàm này.
        /// </summary>
        public static async Task<int> RegisterAsync(Customer data)
        {
            // Kiểm tra email tồn tại thông qua Repository
            if (await customerAccountDB.ExistsAsync(data.Email))
                return -1;

            return await customerAccountDB.AddAsync(data);
        }

        /// <summary>
        /// Lấy thông tin khách hàng theo ID
        /// </summary>
        public static async Task<Customer?> GetCustomerAsync(int customerID)
        {
            return await customerAccountDB.GetAsync(customerID);
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân
        /// </summary>
        public static async Task<bool> UpdateProfileAsync(Customer data)
        {
            return await customerAccountDB.UpdateAsync(data);
        }

        /// <summary>
        /// Đổi mật khẩu.
        /// Hai tham số mật khẩu truyền vào phải là chuỗi ĐÃ MÃ HÓA.
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(int customerID, string hashedOldPassword, string hashedNewPassword)
        {
            var customer = await customerAccountDB.GetAsync(customerID);
            if (customer == null)
                return false;

            // So sánh mật khẩu cũ (cả hai đều đã ở dạng mã hóa)
            if (customer.Password != hashedOldPassword)
                return false;

            return await customerAccountDB.ChangePasswordAsync(customerID, hashedNewPassword);
        }
    }
}