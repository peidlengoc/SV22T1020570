using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;

namespace SV22T1020570.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const string PRODUCT_SEARCH = "ProductSearchInput";

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);

            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 12,
                    SearchValue = "",
                    CategoryID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }

            ViewBag.Categories = CatalogDataService
                .ListCategoriesAsync(new PaginationSearchInput() { Page = 1, PageSize = 0 })
                .Result;

            return View(input);
        }
       
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            // tối ưu: tránh load full
            if (input.PageSize == 0)
                input.PageSize = 12;

            var result = await CatalogDataService.ListProductsAsync(input);

            // lưu session
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);

            return PartialView( "Search",result);
        }

       
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);

            if (product == null)
                return RedirectToAction("Index");

            // load thêm ảnh + thuộc tính
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            return RedirectToAction("AddToCart", "Order", new
            {
                productId = productId,
                quantity = quantity
            });
        }
    }
}