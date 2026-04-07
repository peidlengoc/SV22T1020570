using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy vấn dữ liệu đối với bảng Customers trong SQL Server.
    /// Cài đặt interface ICustomerRepository và sử dụng thư viện Dapper để thao tác dữ liệu.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository với chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách khách hàng theo điều kiện tìm kiếm và trả về kết quả phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dạng PagedResult</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string where = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = @"WHERE CustomerName LIKE @SearchValue
                          OR ContactName LIKE @SearchValue
                          OR Phone LIKE @SearchValue
                          OR Email LIKE @SearchValue";
            }

            string countSql = $@"
                SELECT COUNT(*)
                FROM Customers
                {where}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" });

            if (input.PageSize == 0)
            {
                string sql = $@"
                    SELECT CustomerID, CustomerName, ContactName,
                           Province, Address, Phone, Email, IsLocked
                    FROM Customers
                    {where}
                    ORDER BY CustomerName";

                var data = await connection.QueryAsync<Customer>(
                    sql,
                    new { SearchValue = $"%{input.SearchValue}%" });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = $@"
                    SELECT CustomerID, CustomerName, ContactName,
                           Province, Address, Phone, Email, IsLocked
                    FROM Customers
                    {where}
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Customer>(
                    sql,
                    new
                    {
                        SearchValue = $"%{input.SearchValue}%",
                        Offset = input.Offset,
                        PageSize = input.PageSize
                    });

                result.DataItems = data.ToList();
            }

            return result;
        }

        /// <summary>
        /// Lấy thông tin một khách hàng theo CustomerID
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Đối tượng Customer nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT CustomerID, CustomerName, ContactName,
                                  Province, Address, Phone, Email, IsLocked
                           FROM Customers
                           WHERE CustomerID = @CustomerID";

            return await connection.QueryFirstOrDefaultAsync<Customer>(
                sql,
                new { CustomerID = id });
        }

        /// <summary>
        /// Thêm mới một khách hàng vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần thêm</param>
        /// <returns>Mã CustomerID của bản ghi vừa được tạo</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Customers
                (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES
                (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin của một khách hàng
        /// </summary>
        /// <param name="data">Thông tin khách hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Customers
                SET
                    CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }
        
        /// <summary>
        /// Xóa khách hàng theo CustomerID
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Customers
                           WHERE CustomerID = @CustomerID";

            int rows = await connection.ExecuteAsync(sql, new { CustomerID = id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra khách hàng có dữ liệu liên quan trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE CustomerID = @CustomerID";

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { CustomerID = id });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra một email có hợp lệ hay không (không bị trùng)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// id = 0: kiểm tra khi thêm mới.
        /// id ≠ 0: kiểm tra khi cập nhật (bỏ qua chính bản ghi đó).
        /// </param>
        /// <returns>True nếu email hợp lệ (không trùng)</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql;

            if (id == 0)
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @Email";
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Customers
                        WHERE Email = @Email AND CustomerID <> @CustomerID";
            }

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { Email = email, CustomerID = id });

            return count == 0;
        }
    }
}

