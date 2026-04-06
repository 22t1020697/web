using SV22T1020697.Models.Sales;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020697.Shop
{
    public static class ShoppingCartHelper
    {
        // 1. Đảm bảo tên biến hằng số là duy nhất (Dùng CART_KEY cho toàn bộ file)
        private const string CART_KEY = "ShoppingCart";

        /// <summary>
        /// Lấy giỏ hàng từ session
        /// </summary>
        public static List<CartItem> GetShoppingCart()
        {
            // Thay CART bằng CART_KEY
            var cart = ApplicationContext.GetSessionData<List<CartItem>>(CART_KEY);

            // Xử lý null để tránh lỗi Possible null reference assignment
            if (cart == null)
            {
                cart = new List<CartItem>();
                ApplicationContext.SetSessionData(CART_KEY, cart);
            }
            return cart;
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        public static void AddItemToCart(CartItem item)
        {
            var cart = GetShoppingCart();
            var existItem = cart.Find(m => m.ProductID == item.ProductID);

            if (existItem == null)
            {
                cart.Insert(0,item);
            }
            else
            {
                existItem.Quantity += item.Quantity;
                // Lưu ý: Không cộng dồn đơn giá SalePrice ở đây
                cart.Remove(existItem);
                cart.Insert(0, existItem);
            }
            ApplicationContext.SetSessionData(CART_KEY, cart);
        }

        /// <summary>
        /// Cập nhật số lượng mặt hàng trong giỏ
        /// </summary>
        public static void UpdateCartItem(int productID, int quantity)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity;
                cart.Remove(item);
                cart.Insert(0, item);
                ApplicationContext.SetSessionData(CART_KEY, cart);
            }
        }

        /// <summary>
        /// Xóa một mặt hàng khỏi giỏ
        /// </summary>
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART_KEY, cart);
            }
        }

        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            // Khởi tạo một list mới thay vì truyền null để an toàn
            ApplicationContext.SetSessionData(CART_KEY, new List<CartItem>());
        }
    }
}