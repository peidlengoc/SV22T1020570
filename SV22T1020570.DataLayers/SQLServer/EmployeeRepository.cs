using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.HR;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy vấn dữ liệu đối với bảng Employees trong SQL Server.
    /// Cài đặt interface IEmployeeRepository và sử dụng thư viện Dapper để thao tác dữ liệu.
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository với chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Truy vấn danh sách nhân viên theo điều kiện tìm kiếm và trả về kết quả dạng phân trang
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dạng PagedResult</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Employee>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string where = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                where = @"WHERE FullName LIKE @SearchValue
                          OR Phone LIKE @SearchValue
                          OR Email LIKE @SearchValue";
            }

            string countSql = $@"
                SELECT COUNT(*)
                FROM Employees
                {where}";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = $"%{input.SearchValue}%" });

            if (input.PageSize == 0)
            {
                string sql = $@"
                    SELECT EmployeeID, FullName, BirthDate,
                           Address, Phone, Email, Photo, IsWorking
                    FROM Employees
                    {where}
                    ORDER BY FullName";

                var data = await connection.QueryAsync<Employee>(
                    sql,
                    new { SearchValue = $"%{input.SearchValue}%" });

                result.DataItems = data.ToList();
            }
            else
            {
                string sql = $@"
                    SELECT EmployeeID, FullName, BirthDate,
                           Address, Phone, Email, Photo, IsWorking
                    FROM Employees
                    {where}
                    ORDER BY FullName
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var data = await connection.QueryAsync<Employee>(
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
        /// Lấy thông tin một nhân viên theo EmployeeID
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Đối tượng Employee nếu tồn tại, ngược lại trả về null</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT EmployeeID, FullName, BirthDate,
                                  Address, Phone, Email, Photo, IsWorking
                           FROM Employees
                           WHERE EmployeeID = @EmployeeID";

            return await connection.QueryFirstOrDefaultAsync<Employee>(
                sql,
                new { EmployeeID = id });
        }

        /// <summary>
        /// Thêm mới một nhân viên vào cơ sở dữ liệu
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần thêm</param>
        /// <returns>Mã EmployeeID của bản ghi vừa được tạo</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Employees
                (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                VALUES
                (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Employees
                SET
                    FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking
                WHERE EmployeeID = @EmployeeID";

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa nhân viên theo EmployeeID
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"DELETE FROM Employees
                           WHERE EmployeeID = @EmployeeID";

            int rows = await connection.ExecuteAsync(sql, new { EmployeeID = id });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra nhân viên có dữ liệu liên quan trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"SELECT COUNT(*)
                           FROM Orders
                           WHERE EmployeeID = @EmployeeID";

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { EmployeeID = id });

            return count > 0;
        }

        /// <summary>
        /// Kiểm tra email của nhân viên có hợp lệ (không bị trùng) hay không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// id = 0: kiểm tra khi thêm mới.
        /// id ≠ 0: kiểm tra khi cập nhật (bỏ qua chính bản ghi đó).
        /// </param>
        /// <returns>True nếu email hợp lệ</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql;

            if (id == 0)
            {
                sql = @"SELECT COUNT(*)
                        FROM Employees
                        WHERE Email = @Email";
            }
            else
            {
                sql = @"SELECT COUNT(*)
                        FROM Employees
                        WHERE Email = @Email AND EmployeeID <> @EmployeeID";
            }

            int count = await connection.ExecuteScalarAsync<int>(
                sql,
                new { Email = email, EmployeeID = id });

            return count == 0;
        }
        /// <summary>
        /// Lấy roles của nhân viên
        /// </summary>
        public async Task<string> GetRolesAsync(int employeeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT RoleNames FROM Employees WHERE EmployeeID = @EmployeeID";

                var result = await connection.ExecuteScalarAsync<string>(sql, new { EmployeeID = employeeID });
                return result ?? "";
            }
        }

        /// <summary>
        /// Cập nhật roles cho nhân viên
        /// </summary>
        public async Task<bool> UpdateRoleAsync(int employeeID, string roles)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = @"UPDATE Employees SET RoleNames = @RoleNames WHERE EmployeeID = @EmployeeID";

                    System.Diagnostics.Debug.WriteLine($"=== UpdateRoleAsync ===");
                    System.Diagnostics.Debug.WriteLine($"EmployeeID: {employeeID}");
                    System.Diagnostics.Debug.WriteLine($"Roles to update: '{roles}'");
                    System.Diagnostics.Debug.WriteLine($"SQL: {sql}");

                    var affectedRows = await connection.ExecuteAsync(sql, new
                    {
                        EmployeeID = employeeID,
                        RoleNames = roles ?? ""
                    });

                    System.Diagnostics.Debug.WriteLine($"Affected rows: {affectedRows}");

                    // Kiểm tra lại sau khi update
                    var checkSql = "SELECT RoleNames FROM Employees WHERE EmployeeID = @EmployeeID";
                    var updatedRoles = await connection.ExecuteScalarAsync<string>(checkSql, new { EmployeeID = employeeID });
                    System.Diagnostics.Debug.WriteLine($"Roles after update: '{updatedRoles}'");

                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in UpdateRoleAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Mã hóa mật khẩu (có thể dùng BCrypt hoặc SHA256)
        /// </summary>
        private string HashPassword(string password)
        {
            // Cách 1: Dùng SHA256 (đơn giản)
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }

            // Cách 2: Dùng BCrypt (khuyến nghị)
            // return BCrypt.Net.BCrypt.HashPassword(password);
        }
        /// <summary>
        /// Kiểm tra mật khẩu hiện tại
        /// </summary>
        public async Task<bool> ValidatePasswordAsync(int employeeID, string currentPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"SELECT Password FROM Employees WHERE EmployeeID = @EmployeeID";

                var storedPassword = await connection.ExecuteScalarAsync<string>(sql, new { EmployeeID = employeeID });

                if (string.IsNullOrEmpty(storedPassword))
                    return false;

                // Cách 1: So sánh với SHA256
                var hashedPassword = HashPassword(currentPassword);
                return storedPassword == hashedPassword;

                // Cách 2: Nếu dùng BCrypt
                // return BCrypt.Net.BCrypt.Verify(currentPassword, storedPassword);
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int employeeID, string newPassword)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    // Mã hóa mật khẩu mới
                    var hashedPassword = HashPassword(newPassword);

                    var sql = @"UPDATE Employees 
                               SET Password = @Password 
                               WHERE EmployeeID = @EmployeeID";

                    var affectedRows = await connection.ExecuteAsync(sql, new
                    {
                        EmployeeID = employeeID,
                        Password = hashedPassword
                    });

                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync Error: {ex.Message}");
                return false;
            }
        }
    }
}