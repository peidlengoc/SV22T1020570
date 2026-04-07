using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Sales;

namespace SV22T1020570.BusinessLayers
{
    public class ShoppingCartDBService
    {
        private readonly IShoppingCartRepository _cartDB;
        private readonly IProductRepository _productDB;

        public ShoppingCartDBService(IShoppingCartRepository cartDB,
                                   IProductRepository productDB)
        {
            _cartDB = cartDB;
            _productDB = productDB;
        }

        /// <summary>
        /// Lấy danh sách giỏ hàng
        /// </summary>
        public async Task<List<ShoppingCartItem>> GetCartAsync(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ");

            return await _cartDB.GetCartAsync(customerId);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ
        /// </summary>
        public async Task AddItemAsync(int customerId, int productId, int quantity)
        {
            if (customerId <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ");

            if (productId <= 0)
                throw new ArgumentException("Sản phẩm không hợp lệ");

            if (quantity <= 0)
                throw new ArgumentException("Số lượng phải > 0");

            var product = await _productDB.GetAsync(productId);
            if (product == null)
                throw new ArgumentException("Sản phẩm không tồn tại");

            await _cartDB.AddOrUpdateAsync(customerId, productId, quantity);
        }

        /// <summary>
        /// Cập nhật số lượng
        /// </summary>
        public async Task UpdateItemAsync(int customerId, int productId, int quantity)
        {
            if (customerId <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ");

            if (productId <= 0)
                throw new ArgumentException("Sản phẩm không hợp lệ");

            if (quantity < 0)
                throw new ArgumentException("Số lượng không hợp lệ");

            await _cartDB.UpdateItemAsync(customerId, productId, quantity);
        }

        /// <summary>
        /// Xóa 1 sản phẩm
        /// </summary>
        public async Task RemoveItemAsync(int customerId, int productId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ");

            if (productId <= 0)
                throw new ArgumentException("Sản phẩm không hợp lệ");

            await _cartDB.RemoveItemAsync(customerId, productId);
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        public async Task ClearCartAsync(int customerId)
        {
            if (customerId <= 0)
                throw new ArgumentException("Khách hàng không hợp lệ");

            await _cartDB.ClearCartAsync(customerId);
        }

        /// <summary>
        /// Đếm tổng số lượng sản phẩm trong giỏ
        /// </summary>
        public async Task<int> GetCartCountAsync(int customerId)
        {
            var cart = await _cartDB.GetCartAsync(customerId);
            if (cart == null)
                return 0;

            return cart.Count;
        }

        /// <summary>
        /// Tính tổng tiền
        /// </summary>
        public async Task<decimal> GetCartTotalAsync(int customerId)
        {
            var cart = await _cartDB.GetCartAsync(customerId);
            return cart.Sum(x => x.Quantity * x.SalePrice);
        }
    }
}