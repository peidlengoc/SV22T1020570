using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Security;

namespace SV22T1020570.BusinessLayers
{
    public static class UserAccountService
    {
        private static IUserAccountRepository? _accountDB;

        /// <summary>
        /// Gắn repository vào service (gọi từ Configuration)
        /// </summary>
        public static void Initialize(IUserAccountRepository repository)
        {
            _accountDB = repository;
        }

        /// <summary>
        /// Đăng nhập
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string username, string password)
        {
            if (_accountDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            return await _accountDB.AuthorizeAsync(username, password);
        }

        /// <summary>
        /// Kiểm tra mật khẩu hiện tại của tài khoản
        /// </summary>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu cần kiểm tra</param>
        /// <returns>True nếu mật khẩu đúng, False nếu sai</returns>
        public static async Task<bool> ValidatePasswordAsync(string username, string password)
        {
            if (_accountDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return false;

                return await _accountDB.ValidatePasswordAsync(username, password);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidatePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu (yêu cầu nhập mật khẩu cũ)
        /// </summary>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="currentPassword">Mật khẩu hiện tại</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <returns>True nếu đổi thành công, False nếu thất bại</returns>
        public static async Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (_accountDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            try
            {
                // Kiểm tra mật khẩu hiện tại
                var isValid = await ValidatePasswordAsync(username, currentPassword);
                if (!isValid)
                    return false;

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return false;

                // Không cho phép đặt mật khẩu mới trùng với mật khẩu cũ
                if (currentPassword == newPassword)
                    return false;

                // Cập nhật mật khẩu mới
                return await _accountDB.ChangePasswordAsync(username, newPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cấp mật khẩu mới (dành cho admin, không cần mật khẩu cũ)
        /// </summary>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <returns>True nếu cấp thành công, False nếu thất bại</returns>
        public static async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            if (_accountDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
                    return false;

                if (newPassword.Length < 6)
                    return false;

                return await _accountDB.ChangePasswordAsync(username, newPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResetPasswordAsync Error: {ex.Message}");
                return false;
            }
        }
        // Trong UserAccountService.cs, thêm method mới
        public static async Task<bool> ChangePasswordWithValidationAsync(string username, string currentPassword, string newPassword)
        {
            if (_accountDB == null)
                throw new Exception("UserAccountService chưa được khởi tạo");

            try
            {
                // Kiểm tra mật khẩu hiện tại
                var isValid = await ValidatePasswordAsync(username, currentPassword);
                if (!isValid)
                    return false;

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return false;

                // Không cho phép đặt mật khẩu mới trùng với mật khẩu cũ
                if (currentPassword == newPassword)
                    return false;

                // Cập nhật mật khẩu mới
                return await _accountDB.ChangePasswordAsync(username, newPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePasswordWithValidationAsync Error: {ex.Message}");
                return false;
            }
        }
    }
}