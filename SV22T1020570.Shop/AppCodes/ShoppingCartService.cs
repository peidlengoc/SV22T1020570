using SV22T1020570.Models.Sales;

namespace SV22T1020570.Shop
{
    /// <summary>
    /// Cung cấp các chức năng xử lý trên giỏ hàng
    /// (Giỏ hàng được lưu trong Session)
    /// </summary>
    public static class ShoppingCartService
    {

        /// <summary>
        /// Tên biến để lưu giỏ hàng trong session
        /// </summary>
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy giỏ hàng từ session
        /// </summary>
        /// <returns></returns>
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
        /// Lấy thông tin 1 mặt hàng từ giỏ hàng
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static OrderDetailViewInfo? GetCartItem(int productId)
        {
            var cart = GetShoppingCart();
            return cart.Find(m => m.ProductID == productId);
        }

        /// <summary>
        /// Thêm 1 mặt hàng vào giỏ hàng
        /// </summary>
        /// <param name="item"></param>
        public static void AddCartItem(OrderDetailViewInfo item)
        {
            var cart = GetShoppingCart();
            var existsItem = cart.Find(m => m.ProductID == item.ProductID);
            if (existsItem != null)
            {
                existsItem.Quantity += item.Quantity; //Tăng số lượng
                existsItem.SalePrice = item.SalePrice; // Cập nhật giá
            }
            else
            {
                cart.Add(item);
            }
            ApplicationContext.SetSessionData(CART, cart);
        }

        /// <summary>
        /// Cập nhật số lượng và giá của 1 mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            if (item != null)
            {
                item.Quantity = quantity; //Cập nhật số lượng
                item.SalePrice = salePrice; // Cập nhật giá
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa 1 mặt hàng khỏi giỏ hàng
        /// </summary>
        /// <param name="productId"></param>
        public static void RemoveCartItem(int productId)
        {
            var cart = GetShoppingCart();
            int index = cart.FindIndex(m => m.ProductID == productId);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            ApplicationContext.SetSessionData(CART, new List<OrderDetailViewInfo>());

        }
    }
}
