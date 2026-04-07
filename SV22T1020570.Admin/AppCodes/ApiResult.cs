namespace SV22T1020570.Admin
{
    /// <summary>
    /// Biểu diễn dữ liệu trả về của các API
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Constructor khởi tạo kết quả trả về của API
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiResult(int code, string message = "") 
        {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// Mã kết quả trả về, 0 nếu lỗi, khác 0 nếu thành công
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Thông báo lỗi (nếu có)
        /// </summary>
        public string Message { get; set; } = ""; 
    }
}
