using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.HR;
using System.Threading.Tasks;

namespace SV22T1020570.Admin.Controllers
{
    //[Authorize]
    /// <summary>
    /// Các chức năng liên quan đến nhân viên
    /// </summary>
    public class EmployeeController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong Session
        /// </summary>
        private const string EMPLOYEE_SEARCH = "EmployeeSearchInput";
        /// <summary>
        ///Nhập đầu tìm kiếm, hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {

            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung nhân viên mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch //(Exception ex)
            {
                //TODO: Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await HRDataService.DeleteEmployeeAsync(id);
                    return RedirectToAction("Index");
                }

                //GET
                var model = await HRDataService.GetEmployeeAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await HRDataService.IsUsedEmployeeAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                //Ghi lại log lỗi ex.Message, ex.StackTrace
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");
                ViewBag.CanDelete = false;

                var model = await HRDataService.GetEmployeeAsync(id) ?? new Employee();

                return View("Delete", model);
            }

        }
  
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            ViewBag.Title = "Phân quyền nhân viên";

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            var currentRoles = await HRDataService.GetEmployeeRolesAsync(id);

            ViewBag.CurrentRoles = currentRoles ?? "";
            ViewBag.EmployeeID = id;

            return View(employee);
        }
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, string[] roles)
        {
            System.Diagnostics.Debug.WriteLine($"=== POST ChangeRole ===");
            System.Diagnostics.Debug.WriteLine($"Employee ID: {id}");
            System.Diagnostics.Debug.WriteLine($"Roles array: {(roles != null ? string.Join(", ", roles) : "null")}");
            System.Diagnostics.Debug.WriteLine($"Roles count: {roles?.Length ?? 0}");

            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            // KHÔNG cần Request.Form nữa
            roles = roles ?? new string[0];

            System.Diagnostics.Debug.WriteLine($"Roles after null check: {string.Join(", ", roles)}");

            string roleNames = string.Join(";", roles);
            System.Diagnostics.Debug.WriteLine($"RoleNames to save: '{roleNames}'");

            var result = await HRDataService.UpdateEmployeeRoleAsync(id, roleNames);
            System.Diagnostics.Debug.WriteLine($"Update result: {result}");

            if (!result)
            {
                ModelState.AddModelError("", "Không thể cập nhật quyền");
                ViewBag.CurrentRoles = roleNames;
                ViewBag.EmployeeID = id;
                return View(employee);
            }

            TempData["Success"] = "Cập nhật thành công";
            return RedirectToAction("ChangeRole", new { id });
        }
        /// <summary>
        /// Hiển thị form cấp mật khẩu mới (dành cho admin)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Cấp mật khẩu mới cho nhân viên";

            try
            {
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Nhân viên không tồn tại";
                    return RedirectToAction("Index");
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword GET Error: {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng thử lại sau";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// Xử lý cấp mật khẩu mới (dành cho admin)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string newPassword, string confirmPassword)
        {
            ViewBag.Title = "Cấp mật khẩu mới cho nhân viên";

            try
            {
                // Lấy thông tin nhân viên từ database
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Nhân viên không tồn tại";
                    return RedirectToAction("Index");
                }

                // Validation
                bool hasError = false;

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
                    hasError = true;
                }
                else if (newPassword.Length < 6)
                {
                    ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự");
                    hasError = true;
                }

                if (newPassword != confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                    hasError = true;
                }

                if (hasError || !ModelState.IsValid)
                {
                    // Trả về view với model employee để hiển thị lại thông tin
                    return View(employee);
                }

                // Cập nhật mật khẩu mới
                var result = await HRDataService.ChangeEmployeePasswordAsync(id, newPassword);

                if (!result)
                {
                    ModelState.AddModelError("", "Không thể cập nhật mật khẩu. Vui lòng thử lại sau");
                    return View(employee);
                }

                TempData["Success"] = $"Đã cấp mật khẩu mới cho nhân viên {employee.FullName} thành công";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword POST Error: {ex.Message}");
                ModelState.AddModelError("", "Hệ thống đang bận, vui lòng thử lại sau");

                // Thử lấy lại thông tin nhân viên
                var employee = await HRDataService.GetEmployeeAsync(id);
                if (employee != null)
                {
                    return View(employee);
                }

                return RedirectToAction("Index");
            }
        }
    }
}
