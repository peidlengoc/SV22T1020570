using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng liên quan đến khách hàng
    /// </summary>
    public class CustomerController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong Session
        /// </summary>
        private const string CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        ///Nhập đầu tìm kiếm, hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
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

            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// bổ sung khách hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //Kiểm tra dữ liệu đầu vào có hợp lệ không

            //Sử dụng ModelState để lưu trữ các tình huống (thông báo) lỗi và gửi thông báo lỗi cho View
            //Giả thiết: chỉ cần nhập tên, email, tỉnh thành
            try
            {
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                {
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng!");
                }

                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống!");
                }
                else if (!await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng!");
                }

                if (string.IsNullOrEmpty(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

                //Nếu xuất hiện lỗi trong quá trình nhập thì sẽ ghi nhận dữ liệu không hợp lệ, gửi lại data cũ cho Edit
                if (!ModelState.IsValid)
                    return View("Edit", data);
                //(Tùy chọn) Hiệu chỉnh dữ liệu theo quy tắc của phần mềm
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                //Lưu vào CSDL
                if (data.CustomerID == 0)
                {
                    await PartnerDataService.AddCustomerAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                //Ghi lại log lỗi ex.Message, ex.StackTrace
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await PartnerDataService.DeleteCustomerAsync(id);
                    return RedirectToAction("Index");
                }

                //GET
                var model = await PartnerDataService.GetCustomerAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
                return View(model);
            }
            catch(Exception ex)
{
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");

                ViewBag.CanDelete = false;

                var model = await PartnerDataService.GetCustomerAsync(id) ?? new Customer();

                return View("Delete", model);
            }
        }
        /// <summary>
        /// Đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns></returns>
        public IActionResult ChangePassword(int id)
        {
            return View();
        }
    }
}
