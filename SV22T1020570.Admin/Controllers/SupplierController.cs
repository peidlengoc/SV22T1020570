using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng liên quan đến nhà cung cấp
    /// </summary>
    public class SupplierController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong Session
        /// </summary>
        private const string SUPPLIER_SEARCH = "SupplierSearchInput";
        /// <summary>
        ///Nhập đầu tìm kiếm, hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
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

            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// bổ sung nhà cung cấp mới
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung thông tin nhà cung cấp";
            ViewBag.Provinces = await SelectListHelper.ProvincesAsync();
            return View("Edit", new Supplier());
        }
        /// <summary>
        /// chỉnh sửa/cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="id">mã nhà cung cấp</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";

            //Kiểm tra dữ liệu đầu vào có hợp lệ không

            //Sử dụng ModelState để lưu trữ các tình huống (thông báo) lỗi và gửi thông báo lỗi cho View

            try
            {
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                {
                    ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp!");
                }

                if (string.IsNullOrWhiteSpace(data.ContactName))
                {
                    ModelState.AddModelError(nameof(data.ContactName), "Vui lòng nhập tên giao dịch!");
                }

                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống!");
                }

                if (string.IsNullOrWhiteSpace(data.Phone))
                {
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống!");
                }

                if (string.IsNullOrEmpty(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành");

                //Nếu xuất hiện lỗi trong quá trình nhập thì sẽ ghi nhận dữ liệu không hợp lệ, gửi lại data cũ cho Edit
                if (!ModelState.IsValid)
                    return View("Edit", data);
                //(Tùy chọn) Hiệu chỉnh dữ liệu theo quy tắc của phần mềm

                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                //Lưu vào CSDL
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
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
        /// xóa nhà cung cấp
        /// </summary>
        /// <param name="id">mã nhà cung cấp</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await PartnerDataService.DeleteSupplierAsync(id);
                    return RedirectToAction("Index");
                }

                //GET
                var model = await PartnerDataService.GetSupplierAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedSupplierAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");

                ViewBag.CanDelete = false;

                var model = await PartnerDataService.GetSupplierAsync(id) ?? new Supplier();

                return View("Delete", model);
            }
        }
    }
}
