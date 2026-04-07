using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Catalog;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.HR;
using SV22T1020570.Models.Sales;
using System.Globalization;
using System.Threading.Tasks;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    /// <summary>
    /// Các chức năng liên quan đến nghiệp vụ bán hàng
    /// </summary>
    public class OrderController : Controller
    {
        private const string ORDER_SEARCH = "OrderSearchInput";
        private const string PRODUCT_SEARCH = "ProductSearchInput";
        
        /// <summary>
        ///Nhập đầu vào tìm kiếm và hiển thị kết quả tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
                };
            }

            ViewBag.OrderStatuses = Enum.GetValues(typeof(OrderStatusEnum))
                                        .Cast<OrderStatusEnum>()
                                        .ToList();

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            // ❌ Nếu date không hợp lệ → trả luôn bảng rỗng
            if ((input.DateFrom.HasValue && input.DateFrom.Value.Year < 1753) ||
                (input.DateTo.HasValue && input.DateTo.Value.Year < 1753))
            {
                return PartialView(new PagedResult<OrderViewInfo>()
                {
                    Page = input.Page,
                    PageSize = input.PageSize,
                    RowCount = 0,
                    DataItems = new List<OrderViewInfo>()
                });
            }

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(ORDER_SEARCH, input);

            return PartialView(result);
        }

        /// <summary>
        /// Giao diện gồm các chức năng hỗ trợ cho nghiệp vụ lập đơn hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create() 
        { 
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
                input = new ProductSearchInput() { Page = 1, PageSize = 3 };
            return View(input); 
        }
        
        /// <summary>
        /// Tìm kiếm và hiển thị sản phẩm trong giỏ hàng ở LẬP ĐƠN HÀNG
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> SearchProduct (ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return PartialView(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return PartialView(cart);
        }
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal price)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            
            if (price < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Sản phẩm không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Sản phẩm đã ngưng kinh doanh"));

            ShoppingCartService.AddCartItem(new OrderDetailViewInfo()
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Quantity = quantity,
                SalePrice = price,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            });
            return Json(new ApiResult(1));
        }
        /// <summary>
        ///Hiển thị thông tin chi tiết đơn hàng, đồng thời điều hướng đến các chức năng xử lý trên đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
                return RedirectToAction("Index");

            ViewBag.Employee = await HRDataService.GetEmployeeAsync(order.EmployeeID);
            ViewBag.Customer = await PartnerDataService.GetCustomerAsync(order.CustomerID);
            ViewBag.Shipper = await PartnerDataService.GetShipperAsync(order.ShipperID);
            ViewBag.OrderDetails = await SalesDataService.ListDetailsAsync(id);

            return View(order);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Edit(int id, int customerID, string province, string address)
        {
            try
            {
                // chuẩn hóa dữ liệu
                province = province?.Trim();
                address = address?.Trim();

                // 1. kiểm tra đơn hàng
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null)
                    return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                if (order.Status != OrderStatusEnum.New)
                    return Json(new ApiResult(0, "Không thể chỉnh sửa đơn hàng này"));

                // 2. kiểm tra khách hàng
                if (customerID < 0)
                    return Json(new ApiResult(0, "Khách hàng không hợp lệ"));

                if (customerID > 0)
                {
                    var customer = await PartnerDataService.GetCustomerAsync(customerID);
                    if (customer == null)
                        return Json(new ApiResult(0, "Khách hàng không tồn tại"));

                    if (customer.IsLocked)
                        return Json(new ApiResult(0, "Khách hàng bị khóa"));
                }

                // 3. kiểm tra địa chỉ
                if (string.IsNullOrWhiteSpace(province))
                    return Json(new ApiResult(0, "Vui lòng chọn tỉnh/thành"));

                if (string.IsNullOrWhiteSpace(address))
                    return Json(new ApiResult(0, "Vui lòng nhập địa chỉ"));

                // 4. cập nhật
                var ok = await SalesDataService.UpdateOrderAsync(id, customerID, province, address);
                if (!ok)
                    return Json(new ApiResult(0, "Không thể cập nhật đơn hàng"));

                return Json(new ApiResult(id));
            }
            catch (Exception)
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // ===== GET =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    if (order.Status != OrderStatusEnum.New)
                        return PartialView("_ErrorModal", "Chỉ xóa được đơn chưa duyệt");

                    return PartialView(order);
                }

                // ===== POST =====
                if (Request.Method == "POST")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                    if (order.Status != OrderStatusEnum.New)
                        return Json(new ApiResult(0, "Chỉ xóa được đơn chưa duyệt"));

                    var ok = await SalesDataService.DeleteOrderAsync(id);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể xóa đơn hàng"));

                    return Json(new ApiResult(id));
                }

                return BadRequest();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Cập nhật thông tin (SL, giá) của một mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productId">Mã sản phẩm</param>
        /// <returns></returns>
        public async Task<IActionResult> EditCartItem(int productId)
        {
            try {
                var item = ShoppingCartService.GetCartItem(productId);
                if (item == null)
                    return Json(new ApiResult(0, "Sản phẩm không tồn tại trong giỏ"));

                return PartialView(item);
            } catch {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
            
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng hoặc đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="productId">Mã sản phẩm</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteCartItem(int productId = 0)
        {
            try {
                // POST
                if (Request.Method == "POST")
                {
                    if (productId <= 0)
                        return Json(new ApiResult(0, "Dữ liệu không hợp lệ"));
                    ShoppingCartService.RemoveCartItem(productId);
                    return Json(new ApiResult(1));
                }

                // GET
                var item = ShoppingCartService.GetCartItem(productId);

                return PartialView(item);
            } catch {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
            
        }

        /// <summary>
        /// Làm trống giỏ hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                if (Request.Method == "POST")
                {
                    ShoppingCartService.ClearCart();
                    return Json(new ApiResult(1));
                }
                return PartialView();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));

            }
        }
        
        public async Task<IActionResult> UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            var product = await CatalogDataService.GetProductAsync(productId);

            if (product == null)
                return Json(new ApiResult(0, "Sản phẩm không tồn tại"));
            if (!product.IsSelling)
                return Json(new ApiResult(0, "Sản phẩm đã ngưng kinh doanh"));
            var item = ShoppingCartService.GetCartItem(productId);
            if (item == null)
                return Json(new ApiResult(0, "Sản phẩm không có trong giỏ"));
            ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1));
        }

        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            try
            {
                //Kiểm tra dữ liệu hợp lệ (KH có tồn tại? KH có bị lock tk? Địa chỉ có hợp lệ không? Giỏ hàng có sản phẩm nào không?)
                //1. check khách hàng
                if (customerID != 0)
                {
                    var customer = await PartnerDataService.GetCustomerAsync(customerID);
                    if (customer == null)
                        return Json(new ApiResult(0, "Khách hàng không tồn tại"));

                    if (customer.IsLocked) // hoặc Status tùy bạn định nghĩa
                        return Json(new ApiResult(0, "Tài khoản khách hàng đang bị khóa"));
                }
                //2. check địa chỉ
                if (string.IsNullOrWhiteSpace(province))
                    return Json(new ApiResult(0, "Vui lòng chọn tỉnh/thành"));

                if (string.IsNullOrWhiteSpace(address))
                    return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));
                //3 check giỏ hàng
                var cart = ShoppingCartService.GetShoppingCart();
                if (cart == null || cart.Count == 0)
                    return Json(new ApiResult(0, "Giỏ hàng trống Không lập được đơn hàng"));
                //Tạo đơn hàng mới
                //var order = new Order()
                //{
                //    CustomerID = customerID == 0 ? null : customerID,
                //    DeliveryProvince = province,
                //    DeliveryAddress = address,
                //    OrderTime = DateTime.Now
                //};
                //int orderID = await SalesDataService.AddOrderAsync(order);
                int orderID = await SalesDataService.AddOrderAsync(customerID, province, address);
                if (orderID <= 0)
                    return Json(new ApiResult(0, "Không thể tạo đơn hàng"));
                //bổ sung chi tiết cho đơn hàng

                foreach (var item in cart)
                {
                    item.OrderID = orderID;
                    var ok = await SalesDataService.AddDetailAsync(item);
                    if (!ok)
                    {
                        await SalesDataService.DeleteOrderAsync(orderID); // rollback tay
                        return Json(new ApiResult(0, "Lỗi khi thêm chi tiết đơn hàng"));
                    }
                }
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(orderID));
            }
            catch
            {   
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        /// <summary>
        /// Duyệt đơn hàng (GET: hiển thị modal xác nhận, POST: thực hiện duyệt)
        /// </summary>
        /// <param name="id">Mã đơn hàng cần duyệt</param>
        /// <returns></returns>
        public async Task<IActionResult> Accept(int id)
        {
            try
            {
                // ===== GET: hiển thị modal =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    return PartialView(order); // trả modal confirm
                }

                // ===== POST: xử lý duyệt =====
                if (Request.Method == "POST")
                {
                    var userData = User.GetUserData();
                    if (userData == null)
                        return Json(new ApiResult(0, "Bạn chưa đăng nhập"));

                    int employeeID = int.Parse(userData.UserId);

                    var ok = await SalesDataService.AcceptOrderAsync(id, employeeID);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể duyệt đơn hàng"));

                    return Json(new ApiResult(id));
                }

                return BadRequest("Method không hợp lệ");
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }

        /// <summary>
        /// Chuyển đơn hàng cho người giao hàng (GET: hiển thị modal xác nhận, POST: thực hiện chuyển)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Shipping(int id, int shipperID = 0)
        {
            try
            {
                // ===== GET =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    if (order.Status != OrderStatusEnum.Accepted)
                        return PartialView("_ErrorModal", "Không thể chuyển giao");

                    ViewBag.Shippers = await SelectListHelper.ShippersAsync();
                    return PartialView(order);
                }

                // ===== POST =====
                if (Request.Method == "POST")
                {
                    if (shipperID <= 0)
                        return Json(new ApiResult(0, "Vui lòng chọn người giao hàng"));

                    var ok = await SalesDataService.ShipOrderAsync(id, shipperID);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể giao hàng"));

                    return Json(new ApiResult(id));
                }

                return BadRequest();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        /// <summary>
        /// Hoàn tất đơn hàng (GET: hiển thị modal xác nhận, POST: thực hiện hoàn tất)
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hoàn tất</param>
        /// <returns></returns>
        public async Task<IActionResult> Finish(int id)
        {
            try
            {
                // 👉 GET: mở form modal
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    return PartialView(order); // View: Finish.cshtml
                }

                // 👉 POST: xử lý hoàn tất
                if (Request.Method == "POST")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                    if (order.Status != OrderStatusEnum.Shipping)
                        return Json(new ApiResult(0, "Chỉ đơn đang giao mới được hoàn tất"));

                    var ok = await SalesDataService.CompleteOrderAsync(id);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể hoàn tất đơn hàng"));

                    return Json(new ApiResult(id)); // trả id để reload
                }

                return Json(new ApiResult(0, "Phương thức không hợp lệ"));
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        /// <summary>
        /// Từ chối đơn hàng (GET: hiển thị modal xác nhận, POST: thực hiện từ chối)
        /// </summary>
        /// <param name="id">Mã đơn hàng cần từ chối</param>
        /// <returns></returns>
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                // ===== GET =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    return PartialView(order);
                }

                // ===== POST =====
                if (Request.Method == "POST")
                {
                    var userData = User.GetUserData();
                    if (userData == null)
                        return Json(new ApiResult(0, "Bạn chưa đăng nhập"));

                    int employeeID = int.Parse(userData.UserId);

                    var ok = await SalesDataService.RejectOrderAsync(id, employeeID);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể từ chối đơn hàng"));

                    return Json(new ApiResult(id));
                }

                return BadRequest();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        /// <summary>
        /// Hủy đơn hàng (GET: hiển thị modal xác nhận, POST: thực hiện hủy)
        /// </summary>
        /// <param name="id">Mã đơn hàng cần hủy</param>
        /// <returns></returns>
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                // ===== GET =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    if (order.Status != OrderStatusEnum.New &&
                        order.Status != OrderStatusEnum.Accepted)
                        return PartialView("_ErrorModal", "Không thể hủy đơn hàng này");

                    return PartialView(order);
                }

                // ===== POST =====
                if (Request.Method == "POST")
                {
                    var userData = User.GetUserData();
                    if (userData == null)
                        return Json(new ApiResult(0, "Bạn chưa đăng nhập"));

                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                    if (order.Status != OrderStatusEnum.New &&
                        order.Status != OrderStatusEnum.Accepted)
                        return Json(new ApiResult(0, "Không thể hủy đơn hàng"));

                    var ok = await SalesDataService.CancelOrderAsync(id);
                    if (!ok)
                        return Json(new ApiResult(0, "Không thể hủy đơn hàng"));

                    return Json(new ApiResult(id));
                }

                return BadRequest();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        /// <summary>
        /// Hàm chỉnh sửa sản phẩm trong đơn hàng (GET: hiển thị form chỉnh sửa, POST: thực hiện cập nhật)
        /// </summary>
        /// <param name="id"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task<IActionResult> EditDetail(int id, int productId)
        {
            try
            {
                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null)
                    return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                // chỉ cho sửa khi chưa duyệt
                if (order.Status != OrderStatusEnum.New)
                    return PartialView("_ErrorModal", "Không thể chỉnh sửa đơn hàng này");

                var item = await SalesDataService.GetDetailAsync(id, productId);
                if (item == null)
                    return PartialView("_ErrorModal", "Sản phẩm không tồn tại trong đơn hàng");

                return PartialView(item);
            }
            catch
            {
                return PartialView("_ErrorModal", "Hệ thống lỗi");
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateDetail(int id, int productId, int quantity, decimal salePrice)
        {
            try
            {
                // validate
                if (quantity <= 0)
                    return Json(new ApiResult(0, "Số lượng không hợp lệ"));

                if (salePrice < 0)
                    return Json(new ApiResult(0, "Giá bán không hợp lệ"));

                var order = await SalesDataService.GetOrderAsync(id);
                if (order == null)
                    return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                if (order.Status != OrderStatusEnum.New)
                    return Json(new ApiResult(0, "Không thể chỉnh sửa đơn hàng"));

                var ok = await SalesDataService.UpdateDetailAsync(id, productId, quantity, salePrice);
                if (!ok)
                    return Json(new ApiResult(0, "Không thể cập nhật"));

                return Json(new ApiResult(1));
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
        public async Task<IActionResult> DeleteDetail(int id, int productId)
        {
            try
            {
                // ===== GET =====
                if (Request.Method == "GET")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return PartialView("_ErrorModal", "Đơn hàng không tồn tại");

                    if (order.Status != OrderStatusEnum.New)
                        return PartialView("_ErrorModal", "Không thể xóa sản phẩm");

                    var item = await SalesDataService.GetDetailAsync(id, productId);
                    if (item == null)
                        return Content("Sản phẩm không tồn tại");

                    return PartialView(item);
                }

                // ===== POST =====
                if (Request.Method == "POST")
                {
                    var order = await SalesDataService.GetOrderAsync(id);
                    if (order == null)
                        return Json(new ApiResult(0, "Đơn hàng không tồn tại"));

                    if (order.Status != OrderStatusEnum.New)
                        return Json(new ApiResult(0, "Không thể xóa sản phẩm"));

                    var result = await SalesDataService.DeleteDetailAsync(id, productId);

                    if (result == 0)
                        return Json(new ApiResult(0, "Không thể xóa"));

                    return Json(new ApiResult(result));
                }

                return BadRequest();
            }
            catch
            {
                return Json(new ApiResult(0, "Hệ thống lỗi"));
            }
        }
    }
}
