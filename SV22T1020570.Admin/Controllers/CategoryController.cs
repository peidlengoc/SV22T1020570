using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng liên quan đến loại hàng
    /// </summary>
    public class CategoryController : Controller
    {
        private const string CATEGORY_SEARCH = "CategorySearchInput";
        /// <summary>
        /// Tìm kiếm và hiển thị danh sách phân loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
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
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {

            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Bổ sung loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung loại hàng";
            return View("Edit", new Category());
        }
        /// <summary>
        /// Cập nhật thông tin loại hàng 
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật thông tin loại hàng";

            //Kiểm tra dữ liệu đầu vào có hợp lệ không

            //Sử dụng ModelState để lưu trữ các tình huống (thông báo) lỗi và gửi thông báo lỗi cho View
            //Giả thiết: chỉ cần nhập tên, email, tỉnh thành
            try
            {
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                {
                    ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng!");
                }

                //Nếu xuất hiện lỗi trong quá trình nhập thì sẽ ghi nhận dữ liệu không hợp lệ, gửi lại data cũ cho Edit
                if (!ModelState.IsValid)
                    return View("Edit", data);
                
                //Lưu vào CSDL
                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
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
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteCategoryAsync(id);
                    return RedirectToAction("Index");
                }

                //GET
                var model = await CatalogDataService.GetCategoryAsync(id);
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await CatalogDataService.IsUsedCategoryAsync(id);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");

                ViewBag.CanDelete = false;

                var model = await CatalogDataService.GetCategoryAsync(id) ?? new Category();

                return View("Delete", model);
            }
        }
    }
}
