using SV22T1020697.Models.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020697.DataLayers
{
    public interface ICartRepository
    {
        // Lấy danh sách giỏ hàng của khách từ DB
        IEnumerable<CartItem> GetCartItems(int customerId);
        // Lưu (Đồng bộ) toàn bộ giỏ hàng xuống DB
        bool SyncCart(int customerId, IEnumerable<CartItem> cartItems);
        }
    }

