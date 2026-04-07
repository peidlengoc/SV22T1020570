using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020570.DataLayers.Interfaces;
using SV22T1020570.Models.DataDictionary;

namespace SV22T1020570.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện truy vấn dữ liệu từ bảng Provinces trong SQL Server.
    /// Cài đặt interface IDataDictionaryRepository với entity là Province.
    /// Dùng để lấy danh sách tỉnh/thành trong hệ thống.
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor khởi tạo repository với chuỗi kết nối CSDL
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối SQL Server</param>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách tỉnh/thành từ bảng Provinces
        /// </summary>
        /// <returns>Danh sách các tỉnh/thành</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT ProvinceName
                FROM Provinces
                ORDER BY ProvinceName";

            var data = await connection.QueryAsync<Province>(sql);
            return data.ToList();
        }
    }
}