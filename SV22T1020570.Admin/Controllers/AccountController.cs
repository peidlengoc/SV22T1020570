using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using SV22T1020570.BusinessLayers;
using Microsoft.AspNetCore.Authorization;

namespace SV22T1020570.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        /// <summary>
        /// Hiển thị form đăng nhập
        /// </summary>
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            // 🚨 Validate cơ bản
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            // 🔥 GỌI ĐÚNG SERVICE (async)
            var user = await UserAccountService.AuthorizeAsync(username, password);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            var webUser = new WebUserData
            {
                UserId = user.UserId,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Photo = user.Photo,
                Roles = string.IsNullOrEmpty(user.RoleNames)
                ? new List<string>()
                : user.RoleNames.Split(',').ToList()
            };

            var principal = webUser.CreatePrincipal();

            // 🔥 LOGIN (tạo cookie)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            // 🔥 Redirect đúng (nếu có returnUrl)
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Từ chối truy cập
        /// </summary>
        public IActionResult AccessDenied()
        {
            return View();
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
                {
                    return RedirectToAction("Login");
                }
                // Lấy UserName từ claims
                var username = userData?.UserName;
                if (string.IsNullOrEmpty(username))
                {
                    ModelState.AddModelError("", "Không xác định được tài khoản");
                    return View();
                }

                // Validation
                bool hasError = false;

                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ModelState.AddModelError("currentPassword", "Vui lòng nhập mật khẩu hiện tại");
                    hasError = true;
                }

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
                    return View();
                }

                // Đổi mật khẩu với validation
                var result = await UserAccountService.ChangePasswordWithValidationAsync(username, currentPassword, newPassword);

                if (!result)
                {
                    ModelState.AddModelError("", "Không thể đổi mật khẩu. Vui lòng kiểm tra lại mật khẩu hiện tại");
                    return View();
                }

                // Đổi mật khẩu thành công, đăng xuất và yêu cầu đăng nhập lại
                TempData["Success"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại!";

                await HttpContext.SignOutAsync();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChangePassword Error: {ex.Message}");
                ModelState.AddModelError("", "Hệ thống đang bận, vui lòng thử lại sau");
                return View();
            }
        }

        /// <summary>
        /// Hiển thị thông tin tài khoản
        /// </summary>
        [HttpGet]
        public IActionResult Profile()
        {
            
            var userData = User.GetUserData();
            if (userData == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Title = "Thông tin tài khoản";
            ViewBag.UserId = userData?.UserId;
            ViewBag.DisplayName = userData?.DisplayName;
            ViewBag.Email = userData?.Email;
            ViewBag.Roles = string.Join(", ", userData?.Roles ?? new List<string>());

            return View();
        }
    }
}