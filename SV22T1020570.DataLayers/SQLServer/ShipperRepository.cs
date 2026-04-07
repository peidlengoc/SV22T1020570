using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy vấn dữ liệu đối với bảng Shippers trong SQL Server
    /// Cài đặt interface IGenericRepository với entity là Shipper
    /// Sử dụng thư viện Dapper để thao tác dữ liệu
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository với chuỗi kết nối cơ sở dữ liệu
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách người giao hàng theo điều kiện tìm kiếm
        /// và trả về kết quả dưới dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả tìm kiếm dạng PagedResult</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string where = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = @"WHERE ShipperName LIKE @SearchValue
                          OR Phone LIKE @SearchValue";
            }

            // Đếm tổng số dòng
            string countSql = $@"
                SELECT COUNT(*)
                FROM Shippers
                {where}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" });

            // Không phân trang
            if (input.PageSize == 0)
            {
                string sql = $@"
                    SELECT *
                    FROM Shippers
                    {where}
                    ORDER BY ShipperName";

                var data = await connection.QueryAsync<Shipper>(
                    sql,
                    new { SearchValue = $"%{input.SearchValue}%" });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = $@"
                    SELECT *
                    FROM Shippers
                    {where}
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Shipper>(
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
        /// Lấy thông tin một người giao hàng theo ShipperID
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Đối tượng Shipper nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT *
                           FROM Shippers
                           WHERE ShipperID = @ShipperID";

            return await connection.QueryFirstOrDefaultAsync<Shipper>(
                sql,
                new { ShipperID = id });
        }

        /// <summary>
        /// Thêm mới một người giao hàng vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin người giao hàng cần thêm</param>
        /// <returns>Mã ShipperID của bản ghi vừa được thêm</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Shippers (ShipperName, Phone)
                VALUES (@ShipperName, @Phone);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin của người giao hàng
        /// </summary>
        /// <param name="data">Thông tin người giao hàng cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công, False nếu không tìm thấy dữ liệu</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Shippers
                SET
                    ShipperName = @ShipperName,
                    Phone = @Phone
                WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa người giao hàng theo ShipperID
        /// </summary>
        /// <param name="id">Mã người giao hàng cần xóa</param>
        /// <returns>True nếu xóa thành công, False nếu không tồn tại</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Shippers
                           WHERE ShipperID = @ShipperID";

            int rows = await connection.ExecuteAsync(sql, new { ShipperID = id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu đang được sử dụng, ngược lại False</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE ShipperID = @ShipperID";

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { ShipperID = id });

            return count > 0;
        }
    }
}