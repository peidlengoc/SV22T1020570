using SV22T1020570.DataLayers.SQLServer;

namespace SV22T1020570.BusinessLayers
{
    /// <summary>
    /// lớp này dùng để lưu giữ các thông tin cấu hình sử dụng trong Business Layer
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        ///Biến này lưu lại chuỗi kết nối tham số dữ liệu
        /// </summary>
        private static string _connectionString = "";

        /// <summary>
        /// Hàm này có chức năng khởi tạo cấu hình cho BL
        /// (Hàm này phải được gọi trước khi chạy ứng dụng)
        /// </summary>
        /// <param name="connectionString"></param>
        public static void Initialize (string connectionString)
        {
            _connectionString = connectionString;
            UserAccountService.Initialize(
                new EmployeeAccountRepository(connectionString)
            );
        }

        /// <summary>
        /// Lấy chuỗi tham số kết nối đến CSDL
        /// Muốn lấy chuỗi tham số kết nối để dùng thì sử dụng: (Configuration.ConnectionString)
        /// </summary>
        public static string ConnectionString => _connectionString;
    }
}
