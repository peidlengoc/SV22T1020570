using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.Common;
using SV22T1020570.Models.Catalog;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy vấn dữ liệu đối với bảng Products,
    /// ProductAttributes và ProductPhotos trong SQL Server.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository
        /// </summary>
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách mặt hàng
        /// </summary>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            var result = new PagedResult<Product>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                conditions.Add("ProductName LIKE @SearchValue");
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            if (input.CategoryID > 0)
            {
                conditions.Add("CategoryID = @CategoryID");
                parameters.Add("CategoryID", input.CategoryID);
            }

            if (input.SupplierID > 0)
            {
                conditions.Add("SupplierID = @SupplierID");
                parameters.Add("SupplierID", input.SupplierID);
            }

            if (input.MinPrice > 0)
            {
                conditions.Add("Price >= @MinPrice");
                parameters.Add("MinPrice", input.MinPrice);
            }

            if (input.MaxPrice > 0)
            {
                conditions.Add("Price <= @MaxPrice");
                parameters.Add("MaxPrice", input.MaxPrice);
            }

            string where = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : "";

            string countSQL = $"""
                SELECT COUNT(*)
                FROM Products
                {where}
                """;

            result.RowCount = await connection.ExecuteScalarAsync<int>(countSQL, parameters);

            string querySQL = $"""
                SELECT *
                FROM Products
                {where}
                ORDER BY ProductName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                """;

            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize);

            var data = await connection.QueryAsync<Product>(querySQL, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng
        /// </summary>
        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT *
                FROM Products
                WHERE ProductID = @ProductID
                """;

            return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
        }

        /// <summary>
        /// Thêm mặt hàng
        /// </summary>
        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                INSERT INTO Products
                (ProductName, ProductDescription, SupplierID, CategoryID,
                 Unit, Price, Photo, IsSelling)
                VALUES
                (@ProductName, @ProductDescription, @SupplierID, @CategoryID,
                 @Unit, @Price, @Photo, @IsSelling);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật mặt hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                UPDATE Products
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID
                """;

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                DELETE FROM Products
                WHERE ProductID = @ProductID
                """;

            int rows = await connection.ExecuteAsync(sql, new { ProductID = productID });
            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra mặt hàng đã có dữ liệu liên quan chưa
        /// </summary>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT COUNT(*)
                FROM OrderDetails
                WHERE ProductID = @ProductID
                """;

            int count = await connection.ExecuteScalarAsync<int>(sql, new { ProductID = productID });
            return count > 0;
        }

        // ======================
        // ATTRIBUTE
        // ======================

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT *
                FROM ProductAttributes
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder
                """;

            var data = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT *
                FROM ProductAttributes
                WHERE AttributeID = @AttributeID
                """;

            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                INSERT INTO ProductAttributes
                (ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES
                (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                """;

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                UPDATE ProductAttributes
                SET AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder
                WHERE AttributeID = @AttributeID
                """;

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                DELETE FROM ProductAttributes
                WHERE AttributeID = @AttributeID
                """;

            int rows = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return rows > 0;
        }

        // ======================
        // PHOTO
        // ======================

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT *
                FROM ProductPhotos
                WHERE ProductID = @ProductID
                ORDER BY DisplayOrder
                """;

            var data = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                SELECT *
                FROM ProductPhotos
                WHERE PhotoID = @PhotoID
                """;

            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                INSERT INTO ProductPhotos
                (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES
                (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);

                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
                """;

            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                UPDATE ProductPhotos
                SET Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID
                """;

            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = """
                DELETE FROM ProductPhotos
                WHERE PhotoID = @PhotoID
                """;

            int rows = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return rows > 0;
        }
    }
}
