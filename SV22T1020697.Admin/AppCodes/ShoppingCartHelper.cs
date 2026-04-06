using SV22T1020697.Models.Sales;

namespace SV22T1020697.Admin
{
    /// <summary>
    /// lớp cung cấp các chức năng sử lên trên giỏ hàng
    /// (giỏ hàng lưu trong session)
    /// </summary>
    public static class ShoppingCartHelper
    {
        private const string CART = "ShoppingCart";
        //laáy giỏ hàng từ session
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        /// <summary>
        /// lấy dc thông tin của mặt hàng từ giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            return item;
        }


        /// <summary>
        /// thêm hàng vào giỏ 
        /// </summary>
        /// <param name="item"></param>
        public static void AddItemToCart(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var exisItem = cart.Find(m=>m.ProductID == item.ProductID);
            if (exisItem == null)
            {
                cart.Add(item);
            }
            else
            {
                exisItem.Quantity = item.Quantity;
                exisItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(CART, cart);
        }
        
        /// <summary>
        /// caapj nhat gia va so luong cua mat han trong gio hang
        /// </summary>
         
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m=> m.ProductID == productID);
        }
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m=>m.ProductID == productID);
            if(index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }
        /// <summary>
        /// xóa toàn bộ giỏ hàng 
        /// </summary>

        public static void ClearCart()
        { 
            var newCart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, newCart);
        }
    }
}
