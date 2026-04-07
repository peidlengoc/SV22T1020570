using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.DataLayers.SQLServer;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Sales;

namespace SV22T1020570.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;
        private static readonly ShoppingCartDBService cartService;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
            var productDB = new ProductRepository(Configuration.ConnectionString);
            var cartDB = new ShoppingCartRepository(Configuration.ConnectionString);

            cartService = new ShoppingCartDBService(cartDB, productDB);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Luồng tạo đơn hàng từ phía nhân viên, có thể truyền vào customerID có thể null, province, address, nhưng nếu không truyền thì hệ thống sẽ tự động xử lý
        /// </summary>
        public static async Task<int> AddOrderAsync(int customerID = 0, string province = "", string address = "")
        {
            var order = new Order
            {
                CustomerID = customerID > 0 ? customerID : null,
                DeliveryProvince = string.IsNullOrWhiteSpace(province) ? null : province,
                DeliveryAddress = string.IsNullOrWhiteSpace(address) ? null : address,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };
            return await orderDB.AddAsync(order);
        }
        /// <summary>
        /// Luồng tạo đơn hàng từ phía khách hàng, chỉ cần truyền vào customerID không thể null, province, address, còn các trường khác sẽ do hệ thống tự động xử lý
        /// </summary>
        /// <param name="customerID"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<int> AddOrderFromCustomerAsync(int customerID, string province, string address)
        {
            if (customerID <= 0)
                throw new Exception("Invalid customer");

            // 🔥 1. Lấy giỏ hàng
            var cart = await cartService.GetCartAsync(customerID);

            if (cart == null || cart.Count == 0)
                throw new Exception("Giỏ hàng trống");

            // 🔥 2. Tạo Order
            var order = new Order
            {
                CustomerID = customerID,
                DeliveryProvince = province,
                DeliveryAddress = address,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };

            int orderID = await orderDB.AddAsync(order);

            // 🔥 3. Insert OrderDetail (QUAN TRỌNG NHẤT)
            foreach (var item in cart)
            {
                var detail = new OrderDetail
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.SalePrice
                };

                await orderDB.AddDetailAsync(detail); // 👈 cần có hàm này
            }

            // 🔥 4. Clear cart
            await cartService.ClearCartAsync(customerID);

            return orderID;
        }
        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(int id, int customerID, string province, string address)
        {
            var order = await orderDB.GetAsync(id);
            if (order == null)
                return false;

            // chỉ cho sửa khi chưa duyệt
            if (order.Status != OrderStatusEnum.New)
                return false;

            order.CustomerID = customerID > 0 ? customerID : null;
            order.DeliveryProvince = province;
            order.DeliveryAddress = address;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// (chỉ cho phép xóa khi đơn hàng chưa được duyệt, tức là ở trạng thái New)
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;
            if (shipperID <= 0)
                return false;
            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;
            
            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) 
                return false;

            if (order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;
            
            return await orderDB.UpdateAsync(order);
        }
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersByCustomerAsync(int customerID, OrderSearchInput input)
        {
            return await orderDB.ListByCustomerAsync(customerID, input);
        }
        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// (chỉ cho phép thêm khi đơn hàng chưa được duyệt, tức là ở trạng thái New)
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            if (data.Quantity <= 0 || data.SalePrice < 0)
                return false;

            return await orderDB.AddDetailAsync(data);
        }

        public static async Task<bool> UpdateDetailAsync(int orderId, int productId, int quantity, decimal salePrice)
        {
            // 1. check order
            var order = await orderDB.GetAsync(orderId);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            // 2. validate input
            if (quantity <= 0 || salePrice < 0)
                return false;

            // 3. check product
            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null || !product.IsSelling)
                return false;

            // 4. check detail tồn tại
            var detail = await orderDB.GetDetailAsync(orderId, productId);
            if (detail == null)
                return false;

            // 5. nếu không đổi gì → OK luôn
            if (detail.Quantity == quantity && detail.SalePrice == salePrice)
                return true;

            // 6. update
            return await orderDB.UpdateDetailAsync(orderId, productId, quantity, salePrice);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// (chỉ cho phép xóa khi đơn hàng chưa được duyệt, tức là ở trạng thái New)
        /// </summary>
        public static async Task<int> DeleteDetailAsync(int orderID, int productID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return 0;

            if (order.Status != OrderStatusEnum.New)
                return 0;

            // 1. xóa item
            var ok = await orderDB.DeleteDetailAsync(orderID, productID);
            if (!ok)
                return 0;

            // 2. check còn item không
            var details = await orderDB.ListDetailsAsync(orderID);

            if (details.Count == 0)
            {
                //xóa luôn đơn
                await orderDB.DeleteAsync(orderID);
                return -1; // trả về -1 để biết là đã xóa đơn luôn
            }

            return orderID;
        }
        #endregion
    }
}