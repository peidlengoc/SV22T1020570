using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Sales;


namespace SV22T1020570.Shop.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng mua hàng dành cho khách hàng (giỏ hàng DB)
    /// </summary>
    public class OrderController : Controller
    {

        private readonly SV22T1020570.BusinessLayers.ShoppingCartDBService _cartService;

        public OrderController(ShoppingCartDBService cartService)
        {
            _cartService = cartService;
        }

        private int GetCustomerId()
        {
            var userData = User.GetUserData();
            return int.TryParse(userData?.UserId, out int id) ? id : 0;
        }


        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        public async Task<IActionResult> Cart()
        {
            var customerId = GetCustomerId();
            var cart = await _cartService.GetCartAsync(customerId);
            return View(cart);
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                if (quantity <= 0)
                    return Json(new ApiResult(0, "Số lượng không hợp lệ"));

                var customerId = GetCustomerId();
                if (customerId <= 0)
                    return Json(new ApiResult(0, "Bạn chưa đăng nhập"));

                await _cartService.AddItemAsync(customerId, productId, quantity);

                return Json(new ApiResult(1));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int productId, int quantity)
        {
            try
            {
                if (quantity < 0)
                    return Json(new ApiResult(0, "Số lượng không hợp lệ"));

                var customerId = GetCustomerId();
                await _cartService.UpdateItemAsync(customerId, productId, quantity);

                return Json(new ApiResult(1));
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Xóa 1 sản phẩm khỏi giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeleteCartItem(int productId)
        {
            try
            {
                if (productId <= 0)
                    return Json(new ApiResult(0, "Dữ liệu không hợp lệ"));

                var customerId = GetCustomerId();
                await _cartService.RemoveItemAsync(customerId, productId);

                return Json(new ApiResult(1));
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Xóa toàn bộ giỏ hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var customerId = GetCustomerId();
                await _cartService.ClearCartAsync(customerId);

                return Json(new ApiResult(1));
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Checkout (GET)
        /// </summary>
        public async Task<IActionResult> Checkout()
        {
            var customerId = GetCustomerId();

            if (customerId == 0)
                return RedirectToAction("Login", "Account");

            var cart = await _cartService.GetCartAsync(customerId);

            if (cart == null || cart.Count == 0)
                return RedirectToAction("Cart");

            // 🔥 LẤY PROFILE TỪ DB
            var customer = await PartnerDataService.GetCustomerAsync(customerId);

            ViewBag.Customer = customer;
            ViewBag.Provinces = await SelectListHelper.ProvincesAsync(); 

            return View(cart);
        }

        /// <summary>
        /// Checkout (POST)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryAddress, string deliveryProvince)
        {
            var customerId = GetCustomerId();

            if (customerId == 0)
                return RedirectToAction("Login", "Account");

            var cart = await _cartService.GetCartAsync(customerId);

            if (cart == null || cart.Count == 0)
                return RedirectToAction("Cart");

            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                ModelState.AddModelError("", "Vui lòng nhập địa chỉ giao hàng");
                return View(cart);
            }

            try
            {
                // 🔥 dùng đúng service của mày
                await SalesDataService.AddOrderFromCustomerAsync(customerId, deliveryProvince, deliveryAddress);
                await _cartService.ClearCartAsync(customerId);
                TempData["Success"] = "Đặt hàng thành công";

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(cart);
            }
        }

        /// <summary>
        /// Danh sách đơn hàng
        /// </summary>
        public async Task<IActionResult> Index(OrderSearchInput input)
        {
            var customerId = GetCustomerId();

            if (customerId == 0)
                return RedirectToAction("Login", "Account");

            // fix null input
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = 20,
                    SearchValue = "",
                    Status = 0
                };
            }

            if (input.PageSize == 0)
                input.PageSize = 20;

            var result = await SalesDataService
                .ListOrdersByCustomerAsync(customerId, input);

            return View(result);
        }
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var customerId = GetCustomerId();

            if (customerId == 0)
                return Content("");

            if (input.PageSize == 0)
                input.PageSize = 20;

            var result = await SalesDataService
                .ListOrdersByCustomerAsync(customerId, input);

            return PartialView("Search", result);
        }


        /// <summary>
        /// Chi tiết đơn hàng
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            // 1. lấy order
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            // 2. lấy chi tiết
            var details = await SalesDataService.ListDetailsAsync(id);

            // 3. map sang model của mày (KHÔNG dùng OrderViewInfo)
            var model = new OrderDetailViewModel()
            {
                OrderID = order.OrderID,
                OrderTime = order.OrderTime,
                DeliveryAddress = order.DeliveryAddress,
                DeliveryProvince = order.DeliveryProvince,
                Status = order.Status,

                AcceptTime = order.AcceptTime,
                ShippedTime = order.ShippedTime,
                FinishedTime = order.FinishedTime,

                // 🔥 map từ OrderDetailViewInfo → OrderDetail
                Details = details.Select(x => new OrderDetailViewInfo
                {
                    OrderID = x.OrderID,
                    ProductID = x.ProductID,
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    SalePrice = x.SalePrice
                }).ToList(),

                TotalPrice = details.Sum(x => x.Quantity * x.SalePrice)
            };

            return View(model);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var ok = await SalesDataService.CancelOrderAsync(id);

            return Json(new { code = ok ? 1 : 0 });
        }
    }
}
