using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng liên quan đến mặt hàng
    /// </summary>
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH = "ProductSearchInput";
        /// <summary>
        ///Nhập đầu tìm kiếm, hiển thị kết quả tìm kiếm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);

            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }

            ViewBag.Categories = CatalogDataService
                .ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 })
                .Result;

            ViewBag.Suppliers = PartnerDataService
                .ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 })
                .Result;

            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);

            // lưu session
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            // load combobox
            ViewBag.Categories = await CatalogDataService
                .ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });

            ViewBag.Suppliers = await PartnerDataService
                .ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });

            return PartialView(result);
        }
        /// <summary>
        /// Bổ sung mặt hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";

            ViewBag.Categories = await SelectListHelper.CategoriesAsync();
            ViewBag.Suppliers = await SelectListHelper.SuppliersAsync();

            return View("Edit", new Product());
        }
        /// <summary>
        /// Xem chi tiết mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <returns></returns>
        public IActionResult Detail(int id) { return View(); }
        /// <summary>
        /// Cập nhật thông tin mặt hàng và các thuộc tính, hình ảnh
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin mặt hàng";

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // load combobox
            ViewBag.Categories = await CatalogDataService
                .ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });

            ViewBag.Suppliers = await PartnerDataService
                .ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.ProductID == 0
                ? "Bổ sung mặt hàng"
                : "Cập nhật mặt hàng";

            try
            {
                // ===== VALIDATE =====
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Vui lòng nhập tên mặt hàng");

                if (data.CategoryID == 0)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

                if (data.SupplierID == 0)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Vui lòng nhập đơn vị tính");

                if (data.Price <= 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá phải > 0");

                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });
                    ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });

                    return View("Edit", data);
                }

                // ===== UPLOAD ẢNH =====
                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";


                // ===== SAVE =====
                if (data.ProductID == 0)
                    await CatalogDataService.AddProductAsync(data);
                else
                    await CatalogDataService.UpdateProductAsync(data);

                return RedirectToAction("Index");
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống đang bận");

                ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });
                ViewBag.Suppliers = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 });

                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                //POST
                if (Request.Method == "POST")
                {
                    await CatalogDataService.DeleteProductAsync(id);
                    return RedirectToAction("Index");
                }
                //GET 

                var model = await CatalogDataService.GetProductAsync(id);
                ViewBag.CategoryName = (await CatalogDataService
    .GetCategoryAsync(model.CategoryID))?.CategoryName;

                ViewBag.SupplierName = (await PartnerDataService
                    .GetSupplierAsync(model.SupplierID))?.SupplierName;
                if (model == null)
                    return RedirectToAction("Index");

                ViewBag.CanDelete = !await CatalogDataService.IsUsedProductAsync(id);

                return View(model);
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống tạm thời đang bận, vui lòng thử lại sau");

                ViewBag.CanDelete = false;

                var model = await CatalogDataService.GetProductAsync(id) ?? new Product();

                return View(model);
            }
        }
        //attributes
        /// <summary>
        /// Hiển thị danh sách thuộc tính
        /// </summary>
        /// <param name="id">Mã thuộc tính</param>
        /// <returns></returns>
        public async Task<IActionResult> ListAttributes(int id)
        {
            var data = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.ProductID = id;
            return View(data);
        }
        /// <summary>
        /// Bổ sung thông tin thuộc tính mới cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng cần bổ sung thuộc tính</param>
        /// <returns></returns>
        public IActionResult CreateAttribute(int id)
        {
            ViewBag.ProductID = id;
            ViewBag.Title = "Bổ sung thông tin thuộc tính";
            return View("EditAttribute", new ProductAttribute() { ProductID = id });
        }
        /// <summary>
        /// Cập nhật thông tin thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có thuộc tính cần cập nhật</param>
        /// <param name="attributeId">Mã thuộc tính cần cập nhật</param>
        /// <returns></returns>
        public async Task<IActionResult> EditAttribute(int id, int attributeId)
        {
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null)
                return RedirectToAction("Edit", new { id });

            ViewBag.Title = "Cập nhật thuộc tính của mặt hàng";
            return View("EditAttribute", model);
        }
        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Tên không được rỗng");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được rỗng");
            if (data.DisplayOrder <= 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải lớn hơn giá trị 0");

            if (!ModelState.IsValid)
            {
                data.ProductID = data.ProductID == 0 ? int.Parse(Request.Form["ProductID"]) : data.ProductID;
                return View("EditAttribute", data);
            }    

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            return RedirectToAction("Edit", new { id = data.ProductID });
        }
        /// <summary>
        /// Xóa thuộc tính của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có thuộc tính cần xóa</param>
        /// <param name="attributeId">Mã thuộc tính cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            return RedirectToAction("Edit", new { id });
        }
        //photo
        /// <summary>
        /// Hiển thị danh sách hình ảnh mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var data = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.ProductID = id;
            return View(data);
        }
        /// <summary>
        /// Bổ sung hình ảnh cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có hình ảnh cần bổ sung</param>
        /// <returns></returns>
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung hình ảnh";

            return View("EditPhoto", new ProductPhoto()
            {
                ProductID = id,
                DisplayOrder = 1,
                IsHidden = false
            });
        }
        /// <summary>
        /// Cập nhật thông tin hình ảnh cho mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có hình ảnh cần cập nhật</param>
        /// <param name="photoId">Mã hình ảnh</param>
        /// <returns></returns>
        public async Task<IActionResult> EditPhoto(int id, int photoId)
        {
            var data = await CatalogDataService.GetPhotoAsync(photoId);

            if (data == null)
                return RedirectToAction("Edit", new { id });

            ViewBag.Title = "Cập nhật hình ảnh";

            return View("EditPhoto", data);
        }
        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (data.ProductID == 0)
            {
                data.ProductID = int.Parse(Request.Form["ProductID"]);
            }

            // ===== VALIDATE =====
            if (data.DisplayOrder <= 0)
                ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự phải > 0");

            //Tiền xử lý dữ liệu trước khi lưu vào database
            if (string.IsNullOrEmpty(data.Description)) data.Description = "";
            if (!ModelState.IsValid)
                return View("EditPhoto", data);

            try
            {
                // ===== UPLOAD ẢNH =====
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }

                    data.Photo = fileName;
                }

                // nếu không upload ảnh mới → giữ ảnh cũ
                if (string.IsNullOrEmpty(data.Photo))
                    data.Photo = "nophoto.png";

                // ===== SAVE =====
                if (data.PhotoID == 0)
                    await CatalogDataService.AddPhotoAsync(data);
                else
                    await CatalogDataService.UpdatePhotoAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch
            {
                ModelState.AddModelError("Error", "Hệ thống đang bận");
                return View("EditPhoto", data);
            }
        }
        /// <summary>
        /// Xóa hình ảnh của mặt hàng
        /// </summary>
        /// <param name="id">Mã mặt hàng có hình ảnh cần xóa</param>
        /// <param name="photoId">Mã hình ảnh cần xóa</param>
        /// <returns></returns>
        public async Task<IActionResult> DeletePhoto(int id, int photoId)
        {
            await CatalogDataService.DeletePhotoAsync(photoId);
            return RedirectToAction("Edit", new { id });
        }
    }
}
