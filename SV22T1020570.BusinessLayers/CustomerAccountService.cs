using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.DataLayers.SQLServer;
using SV22T1020570.Models.Security;

namespace SV22T1020570.BusinessLayers
{
    public static class CustomerAccountService
    {
        private static readonly IUserAccountRepository customerDB;

        static CustomerAccountService()
        {
            customerDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        private static string MD5Hash(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public static async Task<UserAccount?> AuthorizeAsync(string username, string password)
        {
            string hashed = MD5Hash(password);
            return await customerDB.AuthorizeAsync(username, hashed);
        }

        public static async Task<bool> RegisterAsync(string email, string password,
                                             string customerName, string contactName, string province)
        {
            var id = await customerDB.RegisterAsync(email, MD5Hash(password), customerName, contactName, province);
            return id > 0;
        }

        public static async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            if (customerDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return false;

                return await customerDB.ValidatePasswordAsync(username, MD5Hash(password));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidatePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (customerDB == null)
                throw new Exception("CustomerAccountService chưa được khởi tạo");

            try
            {
                // ✅ CHECK PASSWORD CŨ (đã hash bên trong rồi)
                var isValid = await ValidatePasswordAsync(username, currentPassword);
                if (!isValid)
                    return false;

                // ✅ CHECK PASSWORD MỚI
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return false;

                // ✅ KHÔNG TRÙNG
                if (currentPassword == newPassword)
                    return false;

                // ✅ HASH PASSWORD MỚI
                string newHash = MD5Hash(newPassword);

                // ✅ UPDATE
                return await customerDB.ChangePasswordAsync(username, newHash);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync Error: {ex.Message}");
                return false;
            }
        }
    }
}