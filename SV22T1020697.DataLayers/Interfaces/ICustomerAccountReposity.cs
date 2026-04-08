using SV22T1020697.Models.Partner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020697.DataLayers.Interfaces
{
    public interface ICustomerAccountRepository
    {
        // Xác thực đăng nhập
        Task<Customer?> LoginAsync(string email, string password);

        // Kiểm tra email tồn tại (dùng cho đăng ký)
        Task<bool> ExistsAsync(string email);

        // Thêm tài khoản mới (đăng ký)
        Task<int> AddAsync(Customer data);

        // Lấy thông tin tài khoản
        Task<Customer?> GetAsync(int customerID);

        // Cập nhật thông tin cá nhân (Profile)
        Task<bool> UpdateAsync(Customer data);

        // Đổi mật khẩu
        Task<bool> ChangePasswordAsync(int customerID, string newPassword);
    }
    }
