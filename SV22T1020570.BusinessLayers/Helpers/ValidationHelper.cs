using System.Text.RegularExpressions;

namespace SV22T1020570.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ kiểm tra dữ liệu hợp lệ
    /// </summary>
    public static class ValidationHelper
    {
        #region Basic Validations

        /// <summary>
        /// Kiểm tra chuỗi không null, rỗng hoặc chỉ chứa khoảng trắng
        /// </summary>
        public static bool IsRequired(string? value, out string errorMessage, string fieldName = "Trường dữ liệu")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = $"{fieldName} không được để trống";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra độ dài chuỗi trong khoảng cho phép
        /// </summary>
        public static bool IsLengthValid(string? value, int minLength, int maxLength, out string errorMessage, string fieldName = "Trường dữ liệu")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = $"{fieldName} không được để trống";
                return false;
            }

            if (value.Length < minLength || value.Length > maxLength)
            {
                errorMessage = $"{fieldName} phải có độ dài từ {minLength} đến {maxLength} ký tự";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra định dạng email hợp lệ
        /// </summary>
        public static bool IsValidEmail(string? email, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = "Email không được để trống";
                return false;
            }

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                if (!regex.IsMatch(email))
                {
                    errorMessage = "Email không đúng định dạng (ví dụ: ten@domain.com)";
                    return false;
                }
            }
            catch
            {
                errorMessage = "Email không hợp lệ";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra định dạng số điện thoại hợp lệ
        /// </summary>
        public static bool IsValidPhone(string? phone, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                errorMessage = "Số điện thoại không được để trống";
                return false;
            }

            var regex = new Regex(@"^[0-9\s\+\(\)\-]{10,15}$");
            if (!regex.IsMatch(phone))
            {
                errorMessage = "Số điện thoại không hợp lệ (chỉ chứa số, khoảng trắng, +, -, () và có độ dài 10-15 ký tự)";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra định dạng số điện thoại (cho phép null hoặc rỗng)
        /// </summary>
        public static bool IsValidPhoneOptional(string? phone, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                errorMessage = string.Empty;
                return true;
            }

            return IsValidPhone(phone, out errorMessage);
        }

        /// <summary>
        /// Kiểm tra định dạng email (cho phép null hoặc rỗng)
        /// </summary>
        public static bool IsValidEmailOptional(string? email, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errorMessage = string.Empty;
                return true;
            }

            return IsValidEmail(email, out errorMessage);
        }

        /// <summary>
        /// Kiểm tra số nguyên dương
        /// </summary>
        public static bool IsPositiveInteger(int? value, out string errorMessage, string fieldName = "Mã")
        {
            if (!value.HasValue || value.Value <= 0)
            {
                errorMessage = $"{fieldName} không hợp lệ";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Kiểm tra giá trị trong khoảng cho phép
        /// </summary>
        public static bool IsInRange(decimal value, decimal minValue, decimal maxValue, out string errorMessage, string fieldName = "Giá trị")
        {
            if (value < minValue || value > maxValue)
            {
                errorMessage = $"{fieldName} phải nằm trong khoảng từ {minValue} đến {maxValue}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        #endregion

        #region Validation Result Class

        /// <summary>
        /// Lớp chứa kết quả validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();

            public ValidationResult()
            {
                IsValid = true;
            }

            public ValidationResult(string error)
            {
                IsValid = false;
                Errors.Add(error);
            }

            public void AddError(string error)
            {
                IsValid = false;
                Errors.Add(error);
            }

            public string GetErrorMessage()
            {
                return string.Join(Environment.NewLine, Errors);
            }
        }

        #endregion

        #region Validation Builder Pattern

        /// <summary>
        /// Lớp xây dựng validation cho đối tượng
        /// </summary>
        public class Validator<T>
        {
            private readonly T _entity;
            private readonly List<string> _errors = new List<string>();
            private bool _isValid = true;

            public Validator(T entity)
            {
                _entity = entity;
            }

            public Validator<T> Require(Func<T, string?> valueSelector, string fieldName)
            {
                var value = valueSelector(_entity);
                if (string.IsNullOrWhiteSpace(value))
                {
                    _isValid = false;
                    _errors.Add($"{fieldName} không được để trống");
                }
                return this;
            }

            public Validator<T> MaxLength(Func<T, string?> valueSelector, int maxLength, string fieldName)
            {
                var value = valueSelector(_entity);
                if (!string.IsNullOrWhiteSpace(value) && value.Length > maxLength)
                {
                    _isValid = false;
                    _errors.Add($"{fieldName} không được vượt quá {maxLength} ký tự");
                }
                return this;
            }

            public Validator<T> MinLength(Func<T, string?> valueSelector, int minLength, string fieldName)
            {
                var value = valueSelector(_entity);
                if (!string.IsNullOrWhiteSpace(value) && value.Length < minLength)
                {
                    _isValid = false;
                    _errors.Add($"{fieldName} phải có ít nhất {minLength} ký tự");
                }
                return this;
            }

            public Validator<T> Email(Func<T, string?> valueSelector, bool required = true)
            {
                var value = valueSelector(_entity);

                if (required && string.IsNullOrWhiteSpace(value))
                {
                    _isValid = false;
                    _errors.Add("Email không được để trống");
                    return this;
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                    if (!regex.IsMatch(value))
                    {
                        _isValid = false;
                        _errors.Add("Email không đúng định dạng");
                    }
                }

                return this;
            }

            public Validator<T> Phone(Func<T, string?> valueSelector, bool required = false)
            {
                var value = valueSelector(_entity);

                if (required && string.IsNullOrWhiteSpace(value))
                {
                    _isValid = false;
                    _errors.Add("Số điện thoại không được để trống");
                    return this;
                }

                if (!string.IsNullOrWhiteSpace(value))
                {
                    var regex = new Regex(@"^[0-9\s\+\(\)\-]{10,15}$");
                    if (!regex.IsMatch(value))
                    {
                        _isValid = false;
                        _errors.Add("Số điện thoại không hợp lệ");
                    }
                }

                return this;
            }

            public Validator<T> Custom(Func<T, bool> predicate, string errorMessage)
            {
                if (!predicate(_entity))
                {
                    _isValid = false;
                    _errors.Add(errorMessage);
                }
                return this;
            }

            public Validator<T> CustomAsync(Func<T, Task<bool>> predicateAsync, string errorMessage)
            {
                // Note: This method should be called with await in async context
                // Usage: await validator.CustomAsync(...).ValidateAsync()
                var task = Task.Run(async () =>
                {
                    if (!await predicateAsync(_entity))
                    {
                        _isValid = false;
                        _errors.Add(errorMessage);
                    }
                });
                task.Wait();
                return this;
            }

            public ValidationResult Validate()
            {
                return new ValidationResult
                {
                    IsValid = _isValid,
                    Errors = _errors
                };
            }

            public async Task<ValidationResult> ValidateAsync()
            {
                return await Task.FromResult(Validate());
            }
        }

        public static Validator<T> Create<T>(T entity)
        {
            return new Validator<T>(entity);
        }

        #endregion
    }
}