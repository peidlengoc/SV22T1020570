using SV22T1020570.Models.Sales;

namespace SV22T1020570.DataLayers.Interfaces
{
    public interface IShoppingCartRepository
    {
        /// <summary>
        /// Hàm lấy danh sách mặt hàng trong giỏ hàng của khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        Task<List<ShoppingCartItem>> GetCartAsync(int customerId);
        /// <summary>
        /// Hàm bổ sung hoặc cập nhật mặt hàng trong giỏ hàng của khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task AddOrUpdateAsync(int customerId, int productId, int quantity);
        /// <summary>
        /// Hàm cập nhật số lượng của một mặt hàng trong giỏ hàng của khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        Task UpdateItemAsync(int customerId, int productId, int quantity);
        /// <summary>
        /// Hàm xóa một mặt hàng khỏi giỏ hàng của khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        Task RemoveItemAsync(int customerId, int productId);
        /// <summary>
        /// Hàm xóa toàn bộ giỏ hàng của khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        Task ClearCartAsync(int customerId);
    }
}
