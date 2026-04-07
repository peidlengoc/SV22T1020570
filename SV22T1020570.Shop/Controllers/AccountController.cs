using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020570.BusinessLayers;
using SV22T1020570.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020570.Shop.Controllers
{
    public class AccountController : Controller
    {
        /// <summary>
        ///  đăng nhập
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        
        public async Task<IActionResult> Login(string username, string password, bool rememberMe, string returnUrl = "")
        {
            var user = await CustomerAccountService.AuthorizeAsync(username, password);
            
            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            var claims = new List<System.Security.Claims.Claim>
{
    new(System.Security.Claims.ClaimTypes.Name, user.Email), 
    new(System.Security.Claims.ClaimTypes.Role, user.RoleNames),
    new("UserId", user.UserId.ToString())
};

            var identity = new System.Security.Claims.ClaimsIdentity(claims, "ShopScheme");
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("ShopScheme", principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = rememberMe, 
                ExpiresUtc = rememberMe ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddHours(1)
            });

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("ShopScheme");
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Profile - Hiển thị thông tin tài khoản của người dùng đã đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userData = User.GetUserData();
            if (userData == null)
            {
                return RedirectToAction("Login");
            }

            var customer = await PartnerDataService.GetCustomerAsync(int.Parse(userData.UserId));

            return View(customer);
        }
        /// <summary>
        /// Hiển thị thông tin tài khoản của người dùng đã đăng nhập
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = User.GetUserData();
            if (user == null)
                return RedirectToAction("Login");

            int customerId = int.Parse(user.UserId);

            var customer = await PartnerDataService.GetCustomerAsync(customerId);

            ViewBag.Provinces = await SelectListHelper.ProvincesAsync();

            return View(customer); // 👈 dùng luôn Customer
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile(Customer model)
        {
            var user = User.GetUserData();
            if (user == null)
                return RedirectToAction("Login");

            int customerId = int.Parse(user.UserId);

            // 🔥 VALIDATION
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Vui lòng nhập họ tên");
            if (string.IsNullOrWhiteSpace(model.ContactName))
                ModelState.AddModelError("ContactName", "Vui lòng nhập tên liên hệ");

            if (!model.Email.Contains("@"))
                ModelState.AddModelError("Email", "Email không hợp lệ");

            if (string.IsNullOrWhiteSpace(model.Address))
                ModelState.AddModelError("Address", "Vui lòng nhập địa chỉ");
            if (string.IsNullOrWhiteSpace(model.Province))
                ModelState.AddModelError("Province", "Vui lòng chọn tỉnh/thành");

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await SelectListHelper.ProvincesAsync();
                return View(model);
            }

            var current = await PartnerDataService.GetCustomerAsync(customerId);
            if (current == null)
                return RedirectToAction("Login");

            model.CustomerID = customerId;
            model.IsLocked = current.IsLocked;

            var result = await PartnerDataService.UpdateCustomerAsync(model);

            if (!result)
            {
                ModelState.AddModelError("", "Cập nhật thất bại");
                return View(model);
            }

            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction("EditProfile");
        }

        /// <summary>
        /// Trang AccessDenied - Hiển thị khi người dùng cố gắng truy cập vào một trang mà họ không có quyền
        /// </summary>
        /// <returns></returns>
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ===== REGISTER (GET) =====
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Provinces = await SelectListHelper.ProvincesAsync();
            return View();
        }

        // ===== REGISTER (POST) =====
        [HttpPost]
        public async Task<IActionResult> Register(string email, string password, string confirmPassword,
                                          string customerName, string contactName, string province)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(customerName))
            {
                ViewBag.Error = "Vui lòng nhập tên khách hàng";
                return View();
            }

            if (string.IsNullOrWhiteSpace(contactName))
            {
                ViewBag.Error = "Vui lòng nhập tên liên hệ";
                return View();
            }
            if (string.IsNullOrWhiteSpace(province))
            {
                ViewBag.Error = "Vui lòng chọn tỉnh/thành";
                ViewBag.Provinces = await SelectListHelper.ProvincesAsync(); // ⚠️ nhớ load lại
                return View();
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Vui lòng nhập email";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập mật khẩu";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp";
                return View();
            }

            var isValid = await PartnerDataService.ValidateCustomerEmailAsync(email);

            if (!isValid)
            {
                ViewBag.Error = "Email đã được sử dụng";
                return View();
            }

            // 👉 gọi repository
            var result = await CustomerAccountService.RegisterAsync(email, password, customerName, contactName, province);

            if (!result)
            {
                ViewBag.Error = "Đăng ký thất bại";
                return View();
            }

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Login");
        }
        /// <summary>
        /// Hiển thị form đổi mật khẩu (dành cho người dùng đã đăng nhập)
        /// </summary>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";

            // Kiểm tra người dùng đã đăng nhập chưa
            var userData = User.GetUserData();
            if (userData == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        /// <summary>
        /// Xử lý đổi mật khẩu
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            ViewBag.Title = "Đổi mật khẩu";

            try
            {
                var userData = User.GetUserData();
                if (userData == null)
                    return RedirectToAction("Login");

                var username = userData.UserName;
                

               
                if (string.IsNullOrWhiteSpace(currentPassword))
                    ModelState.AddModelError("currentPassword", "Vui lòng nhập mật khẩu hiện tại");

                if (string.IsNullOrWhiteSpace(newPassword))
                    ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
                else
                {
                    if (newPassword.Length < 6)
                        ModelState.AddModelError("newPassword", "Mật khẩu phải >= 6 ký tự");

                    if (!newPassword.Any(char.IsUpper))
                        ModelState.AddModelError("newPassword", "Phải có chữ hoa");

                    if (!newPassword.Any(char.IsDigit))
                        ModelState.AddModelError("newPassword", "Phải có số");
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                    ModelState.AddModelError("confirmPassword", "Vui lòng nhập lại mật khẩu");

                if (newPassword != confirmPassword)
                    ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

                if (currentPassword == newPassword)
                    ModelState.AddModelError("newPassword", "Mật khẩu mới không được trùng mật khẩu cũ");

                if (!ModelState.IsValid)
                    return View();

                
                var result = await CustomerAccountService.ChangePasswordAsync(username, currentPassword, newPassword);

                if (!result)
                {
                    ModelState.AddModelError("", "Đổi mật khẩu thành công");
                    return View();
                }

               
                TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại!";

                await HttpContext.SignOutAsync("ShopScheme");
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword Error: {ex.Message}");
                ModelState.AddModelError("", "Hệ thống đang bận, vui lòng thử lại sau");
                return View();
            }
        }
    }
}