using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Security;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020570.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mã hóa mật khẩu bằng SHA256
        /// </summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
        SELECT 
            EmployeeID AS UserId,
            Email AS UserName,
            FullName AS DisplayName,
            Email,
            Password,
            'admin' AS RoleNames
        FROM Employees
        WHERE Email = @Email AND Password = @Password
    ";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
            {
                Email = userName,
                Password = password
            });
        }

        /// <summary>
        /// Kiểm tra mật khẩu hiện tại của tài khoản
        /// </summary>
        public async Task<bool> ValidatePasswordAsync(string userName, string password)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"SELECT COUNT(*)
                                   FROM Employees
                                   WHERE Email = @userName AND Password = @password";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@userName", userName);
                        var hashedPassword = HashPassword(password);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        var count = (int)await cmd.ExecuteScalarAsync();
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidatePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu cho tài khoản (không cần kiểm tra mật khẩu cũ)
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Mã hóa mật khẩu mới
                    var hashedPassword = HashPassword(password);

                    string sql = @"UPDATE Employees
                                   SET Password = @password
                                   WHERE Email = @userName";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@userName", userName);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu có kiểm tra mật khẩu cũ (dành cho người dùng)
        /// </summary>
        public async Task<bool> ChangePasswordWithValidationAsync(string userName, string currentPassword, string newPassword)
        {
            try
            {
                // Kiểm tra mật khẩu hiện tại
                var isValid = await ValidatePasswordAsync(userName, currentPassword);
                if (!isValid)
                    return false;

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return false;

                // Không cho phép đặt mật khẩu mới trùng với mật khẩu cũ
                if (currentPassword == newPassword)
                    return false;

                // Cập nhật mật khẩu mới
                return await ChangePasswordAsync(userName, newPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordWithValidationAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin tài khoản theo email
        /// </summary>
        public async Task<UserAccount?> GetAccountByEmailAsync(string email)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"SELECT EmployeeID, FullName, Email, RoleNames
                                   FROM Employees
                                   WHERE Email = @email AND IsWorking = 1";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@email", email);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new UserAccount()
                                {
                                    UserId = reader["EmployeeID"].ToString(),
                                    UserName = reader["Email"].ToString(),
                                    DisplayName = reader["FullName"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    RoleNames = reader["RoleNames"]?.ToString() ?? ""
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAccountByEmailAsync Error: {ex.Message}");
                return null;
            }

            return null;
        }


        public Task<int> RegisterAsync(string email, string password, string customerName, string contactName, string province)
        {
            throw new NotImplementedException();
        }
    }
}