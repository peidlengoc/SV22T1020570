using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác dữ liệu đối với bảng Suppliers trong SQL Server
    /// Cài đặt interface IGenericRepository với entity là Supplier
    /// Sử dụng thư viện Dapper để truy vấn dữ liệu
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository với chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thực hiện truy vấn danh sách nhà cung cấp theo điều kiện tìm kiếm
        /// và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả tìm kiếm dạng PagedResult</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string where = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = @"WHERE SupplierName LIKE @SearchValue
                          OR ContactName LIKE @SearchValue
                          OR Phone LIKE @SearchValue";
            }

            // Đếm tổng số dòng
            string countSql = $@"
                SELECT COUNT(*)
                FROM Suppliers
                {where}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" }
            );

            // Nếu không phân trang
            if (input.PageSize == 0)
            {
                string sql = $@"
                    SELECT *
                    FROM Suppliers
                    {where}
                    ORDER BY SupplierName";

                var data = await connection.QueryAsync<Supplier>(
                    sql,
                    new { SearchValue = $"%{input.SearchValue}%" }
                );

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = $@"
                    SELECT *
                    FROM Suppliers
                    {where}
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Supplier>(
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
        /// Lấy thông tin một nhà cung cấp theo SupplierID
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Đối tượng Supplier nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Suppliers
                           WHERE SupplierID = @SupplierID";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(
                sql,
                new { SupplierID = id });
        }

        /// <summary>
        /// Thêm mới một nhà cung cấp vào CSDL
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần thêm</param>
        /// <returns>Mã SupplierID của bản ghi vừa được tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Suppliers
                (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES
                (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy dữ liệu</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Suppliers
                SET
                    SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa một nhà cung cấp theo SupplierID
        /// </summary>
        /// <param name="id">Mã nhà cung cấp cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu không tồn tại</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Suppliers
                           WHERE SupplierID = @SupplierID";

            int rows = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu đang được sử dụng, ngược lại False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Products
                           WHERE SupplierID = @SupplierID";

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { SupplierID = id });

            return count > 0;
        }
    }
}