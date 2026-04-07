using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.DataLayers.SQLServer;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.HR;

namespace SV22T1020570.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa tìm kiếm, trang cần hiển thị, số dòng mỗi trang).
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách nhân viên có phân trang.
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần tìm.</param>
        /// <returns>
        /// Đối tượng Employee nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào hệ thống.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần bổ sung.</param>
        /// <returns>Mã nhân viên được tạo mới.</returns>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            //TODO: Kiểm tra dữ liệu hợp lệ
            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            //TODO: Kiểm tra dữ liệu hợp lệ
            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, False nếu nhân viên đang được sử dụng
        /// hoặc việc xóa không thực hiện được.
        /// </returns>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra.</param>
        /// <returns>
        /// True nếu nhân viên đang được sử dụng, ngược lại False.
        /// </returns>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            var employee = await GetEmployeeAsync(employeeID);

            if (employee.IsWorking)
                return true;
            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// (không bị trùng với email của nhân viên khác).
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="employeeID">
        /// Nếu employeeID = 0: kiểm tra email đối với nhân viên mới.
        /// Nếu employeeID khác 0: kiểm tra email của nhân viên có mã là employeeID.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), ngược lại False.
        /// </returns>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        public static async Task<Employee?> GetEmployeeAsync(int? employeeID)
        {
            if (!employeeID.HasValue)
                return null;

            return await employeeDB.GetAsync(employeeID.Value);
        }
        /// <summary>
        /// Lấy roles hiện tại của nhân viên
        /// </summary>
        /// <param name="employeeID">Mã nhân viên</param>
        /// <returns>Chuỗi roles phân cách bằng dấu ; (ví dụ: "admin;staff")</returns>
        public static async Task<string> GetEmployeeRolesAsync(int employeeID)
        {
            try
            {
                return await employeeDB.GetRolesAsync(employeeID);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần
                Console.WriteLine($"Error getting roles for employee {employeeID}: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Cập nhật roles cho nhân viên
        /// </summary>
        /// <param name="employeeID">Mã nhân viên</param>
        /// <param name="roles">Chuỗi roles phân cách bằng dấu ;</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public static async Task<bool> UpdateEmployeeRoleAsync(int employeeID, string roles)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HRDataService.UpdateEmployeeRoleAsync - ID: {employeeID}, Roles: '{roles}'");
                return await employeeDB.UpdateRoleAsync(employeeID, roles);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HRDataService.UpdateEmployeeRoleAsync Error: {ex.Message}");
                return false;
            }
        }
        #region Password Management

        /// <summary>
        /// Kiểm tra mật khẩu hiện tại của nhân viên
        /// </summary>
        public static async Task<bool> ValidateEmployeePasswordAsync(int employeeID, string currentPassword)
        {
            try
            {
                return await employeeDB.ValidatePasswordAsync(employeeID, currentPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ValidateEmployeePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu nhân viên
        /// </summary>
        public static async Task<bool> ChangeEmployeePasswordAsync(int employeeID, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return false;

                return await employeeDB.ChangePasswordAsync(employeeID, newPassword);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangeEmployeePasswordAsync Error: {ex.Message}");
                return false;
            }
        }

        #endregion
        #endregion
    }
}