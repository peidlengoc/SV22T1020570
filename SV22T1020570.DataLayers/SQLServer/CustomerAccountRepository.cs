using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Security;
using System.Security.Cryptography;
using System.Text;

namespace SV22T1020570.DataLayers.SQLServer
{
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private string MD5Hash(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    SELECT 
                        CustomerID AS UserId,
                        Email AS UserName,
                        CustomerName AS DisplayName,
                        Email,
                        Password,
                        'customer' AS RoleNames
                    FROM Customers
                    WHERE Email = @Email";

                var user = await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
                {
                    Email = userName
                });

                if (user == null)
                    return null;

                if (user.Password.Trim().ToLower() != password.Trim().ToLower())
                    return null;

                return user;
            }
        }

        public async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                string sql = @"SELECT Password FROM Customers WHERE Email = @Username";

                var dbPassword = await connection.ExecuteScalarAsync<string>(sql, new
                {
                    Username = username
                });

                if (string.IsNullOrEmpty(dbPassword))
                    return false;

                return dbPassword.Trim().ToLower() == password.Trim().ToLower();
            }
        }

        public async Task<bool> ChangePasswordAsync(string username, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = @"
                    UPDATE Customers
                    SET Password = @Password
                    WHERE Email = @Username";

                var affectedRows = await connection.ExecuteAsync(sql, new
                {
                    Username = username,
                    Password = newPassword
                });

                return affectedRows > 0;
            }
        }

        public async Task<int> RegisterAsync(string email, string password, string customerName, string contactName, string province)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO Customers (CustomerName, ContactName, Province, Email, Password, IsLocked)
                    VALUES (@CustomerName, @ContactName, @Province, @Email, @Password, 0);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var id = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    CustomerName = customerName,
                    ContactName = contactName,
                    Province = province,
                    Email = email,
                    Password = password
                });

                return id;
            }
        }
    }
}