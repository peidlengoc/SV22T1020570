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
    /// Các chức năng liên quan đến người giao hàng
    /// </summary>
    public class ShipperController : Controller
    {
        /// <summary>
        /// Tên của biến dùng để lưu điều kiện tìm kiếm khách hàng trong Session
        /// </summary>
        private const string SHIPPER_SEARCH = "ShipperSearchInput";
        /// <summary>
        ///Nhập đầu tìm kiếm, hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
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

            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung người giao hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create() {
            ViewBag.Title = "Bổ sung người giao hàng";
            return View("Edit", new Shipper()); 
        }
        /// <summary>
        /// Cập nhật thông tin người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";

            //Kiểm tra dữ liệu đầu vào có hợp lệ không

            //Sử dụng ModelState để lưu trữ các tình huống (thông báo) lỗi và gửi thông báo lỗi cho View

            try
            {
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                {
                    ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên người giao hàng!");
                }

                if (string.IsNullOrWhiteSpace(data.Phone))
                {
                    ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống!");
                }

                //Nếu xuất hiện lỗi trong quá trình nhập thì sẽ ghi nhận dữ liệu không hợp lệ, gửi lại data cũ cho Edit
                if (!ModelState.IsValid)
                    return View("Edit", data);
               
                //Lưu vào CSDL
                if (data.ShipperID == 0)
                {
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
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
        /// Xóa người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await PartnerDataService.DeleteShipperAsync(id);
                    return RedirectToAction("Index");
                }

                //GET
                var model = await PartnerDataService.GetShipperAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await PartnerDataService.IsUsedShipperAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");

                ViewBag.CanDelete = false;

                var model = await PartnerDataService.GetShipperAsync(id) ?? new Shipper();

                return View("Delete", model);
            }
        }
    }
}
