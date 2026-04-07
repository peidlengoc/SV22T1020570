using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Sales;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Repository thực hiện các thao tác dữ liệu liên quan đến đơn hàng
    /// và chi tiết đơn hàng trong SQL Server
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối
        /// </summary>
        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dạng phân trang
        /// </summary>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (input.CustomerID > 0)
            {
                conditions.Add("o.CustomerID = @CustomerID");
                parameters.Add("CustomerID", input.CustomerID);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                conditions.Add("(c.CustomerName LIKE @Search OR c.Phone LIKE @Search)");
                parameters.Add("Search", $"%{input.SearchValue}%");
            }

            if (input.Status != 0)
            {
                conditions.Add("o.Status = @Status");
                parameters.Add("Status", (int)input.Status);
            }

            if (input.DateFrom != null)
            {
                conditions.Add("o.OrderTime >= @DateFrom");
                parameters.Add("DateFrom", input.DateFrom.Value.Date);
            }

            if (input.DateTo != null)
            {
                conditions.Add("o.OrderTime < @DateTo");
                parameters.Add("DateTo", input.DateTo.Value.Date.AddDays(1));
            }

            string where = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : "";

            string countSql = $"""
                SELECT COUNT(*)
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                {where}
                """;

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            string sql = $"""
                SELECT o.*,
                       c.CustomerName, c.ContactName AS CustomerContactName,
                       c.Email AS CustomerEmail, c.Phone AS CustomerPhone,
                       c.Address AS CustomerAddress,
                       e.FullName AS EmployeeName,
                       s.ShipperName, s.Phone AS ShipperPhone,
                       SUM(d.Quantity * d.SalePrice) AS TotalPrice
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                LEFT JOIN OrderDetails d ON o.OrderID = d.OrderID
                {where}
                GROUP BY o.OrderID, o.CustomerID, o.OrderTime, o.DeliveryProvince,
                         o.DeliveryAddress, o.EmployeeID, o.AcceptTime,
                         o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status,
                         c.CustomerName, c.ContactName, c.Email, c.Phone, c.Address,
                         e.FullName, s.ShipperName, s.Phone
                ORDER BY o.OrderID ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                """;

            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize);

            var data = await connection.QueryAsync<OrderViewInfo>(sql, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin đầy đủ của một đơn hàng
        /// </summary>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT o.*, 
                       c.CustomerName, c.ContactName AS CustomerContactName,
                       c.Email AS CustomerEmail, c.Phone AS CustomerPhone,
                       c.Address AS CustomerAddress,
                       e.FullName AS EmployeeName,
                       s.ShipperName, s.Phone AS ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE o.OrderID = @OrderID
                """;

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        /// <summary>
        /// Thêm đơn hàng
        /// </summary>
        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                INSERT INTO Orders
                (CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, Status)
                VALUES
                (@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, @Status);

                SELECT CAST(SCOPE_IDENTITY() AS INT)
                """;

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                UPDATE Orders
                SET CustomerID = @CustomerID,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status
                WHERE OrderID = @OrderID
                """;

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int orderID)
        {
            //Mở connection thủ công vì transaction cần connection đang mở
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            /// Sử dụng transaction để đảm bảo thao tác xóa diễn ra đồng nhất.
            // Nếu một trong hai lệnh SQL thất bại thì toàn bộ thay đổi sẽ rollback.
            using var transaction = connection.BeginTransaction();

            try
            {
                //Truyền transaction vào Dapper
                await connection.ExecuteAsync(
                    "DELETE FROM OrderDetails WHERE OrderID=@OrderID",
                    new { OrderID = orderID },
                    transaction
                );

                int rows = await connection.ExecuteAsync(
                    "DELETE FROM Orders WHERE OrderID=@OrderID",
                    new { OrderID = orderID },
                    transaction
                );

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT d.*, p.ProductName, p.Unit, p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID = p.ProductID
                WHERE d.OrderID = @OrderID
                """;

            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID });
            return data.ToList();
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT d.*, p.ProductName, p.Unit, p.Photo
                FROM OrderDetails d
                JOIN Products p ON d.ProductID = p.ProductID
                WHERE d.OrderID=@OrderID AND d.ProductID=@ProductID
                """;

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(
                sql, new { OrderID = orderID, ProductID = productID });
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                VALUES (@OrderID,@ProductID,@Quantity,@SalePrice)
                """;

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Cập nhật số lượng và giá bán
        /// </summary>
        public async Task<bool> UpdateDetailAsync(int orderId, int productId, int quantity, decimal salePrice)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
        UPDATE OrderDetails
        SET Quantity = @Quantity,
            SalePrice = @SalePrice
        WHERE OrderID = @OrderID AND ProductID = @ProductID
    """;

            int rows = await connection.ExecuteAsync(sql, new
            {
                OrderID = orderId,
                ProductID = productId,
                Quantity = quantity,
                SalePrice = salePrice
            });

            return rows >= 0; 
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                DELETE FROM OrderDetails
                WHERE OrderID=@OrderID AND ProductID=@ProductID
                """;

            int rows = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
            return rows > 0;
        }
        /// <summary>
        /// Hàm lấy danh sách đơn hàng của một khách hàng, có hỗ trợ tìm kiếm và phân trang
        /// </summary>
        /// <param name="customerID"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<PagedResult<OrderViewInfo>> ListByCustomerAsync(int customerID, OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            // 🔥 BASE WHERE
            string where = "WHERE CustomerID = @CustomerID";

            // 🔍 SEARCH
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where += " AND CAST(OrderID AS NVARCHAR) LIKE @SearchValue";
            }

            // 📌 STATUS
            if (input.Status != 0)
            {
                where += " AND Status = @Status";
            }

            // 📅 DATE FROM
            if (input.DateFrom.HasValue)
            {
                where += " AND OrderTime >= @DateFrom";
            }

            // 📅 DATE TO (fix chuẩn full ngày)
            if (input.DateTo.HasValue)
            {
                where += " AND OrderTime < DATEADD(day, 1, @DateTo)";
            }

            // 🔢 COUNT
            string countSql = $@"
        SELECT COUNT(*)
        FROM Orders
        {where}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSql, new
            {
                CustomerID = customerID,
                SearchValue = $"%{input.SearchValue}%",
                Status = input.Status,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo
            });

            // 📦 DATA
            string sql = $@"
        SELECT *
        FROM Orders
        {where}
        ORDER BY OrderTime DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var data = await connection.QueryAsync<OrderViewInfo>(sql, new
            {
                CustomerID = customerID,
                SearchValue = $"%{input.SearchValue}%",
                Status = input.Status,
                DateFrom = input.DateFrom,
                DateTo = input.DateTo,
                Offset = input.Offset,
                PageSize = input.PageSize
            });

            result.DataItems = data.ToList();

            return result;
        }
    }
}