using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private readonly string _connectionString;

        public VehicleRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new OracleConnection(_connectionString);

        //Khi gọi procedure thì không cần gọi Returning để lấy Id vừa tạo nữa
        //Còn khi sử dụng Với SQL Thuần (Dùng RETURNING)
        public async Task<int> CreateAsync(VehicleDto vehicle)
        {
            using var connection = CreateConnection();

            // BƯỚC 1: Kiểm tra PolicyId có tồn tại trong bảng DHN_POLICY không
            var checkSql = "SELECT COUNT(1) FROM INSURANCE_USER.DHN_POLICY WHERE POLICY_ID = :PolicyId";
            var exists = await connection.ExecuteScalarAsync<int>(checkSql, new { PolicyId = vehicle.PolicyId });

            if (exists == 0)
            {
                throw new KeyNotFoundException($"Policy với ID {vehicle.PolicyId} không tồn tại.");
            }

            // BƯỚC 2: Thực hiện Insert nếu Policy hợp lệ
            var sql = @"
                    BEGIN 
                        INSERT INTO INSURANCE_USER.DHN_VEHICLE (POLICY_ID, LICENSE_PLATE, BRAND, MODEL, YEAR_MANUFACTURED) 
                        VALUES (:PolicyId, :LicensePlate, :Brand, :Model, :YearManufactured) 
                        RETURNING VEHICLE_ID INTO :NewId; 
                    END;";

            var parameters = new DynamicParameters();
            parameters.Add("PolicyId", vehicle.PolicyId);
            parameters.Add("LicensePlate", vehicle.LicensePlate);
            parameters.Add("Brand", vehicle.Brand);
            parameters.Add("Model", vehicle.Model);
            parameters.Add("YearManufactured", vehicle.YearManufactured);

            // Tham số Output để hứng giá trị ID
            parameters.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output, size: 10);

            await connection.ExecuteAsync(sql, parameters);

            var resultId = parameters.Get<int>(":NewId");
            return resultId;
        }

        public async Task<bool> DeleteAsync(int vehicleId)
        {
            var sql = "DELETE FROM INSURANCE_USER.DHN_VEHICLE WHERE VEHICLE_ID = :Id";
            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = vehicleId });
            return rowsAffected > 0;
        }

        public async Task<VehicleDto> GetByIdAsync(int vehicleId)
        {
            var sql = @"SELECT VEHICLE_ID as VehicleId, 
                               POLICY_ID as PolicyId, 
                               LICENSE_PLATE as LicensePlate, 
                               BRAND, MODEL, 
                               YEAR_MANUFACTURED as YearManufactured 
                        FROM INSURANCE_USER.DHN_VEHICLE WHERE VEHICLE_ID = :Id";
            using var connection = CreateConnection();


            return await connection.QuerySingleOrDefaultAsync(sql, new { Id = vehicleId });
        }

        public async Task<IEnumerable<VehicleDto>> GetByPolicyIdAsync(int policyId)
        {
            var sql = @"SELECT VEHICLE_ID as VehicleId, 
                               POLICY_ID as PolicyId, 
                               LICENSE_PLATE as LicensePlate, 
                               BRAND, MODEL, 
                               YEAR_MANUFACTURED as YearManufactured 
                        FROM INSURANCE_USER.DHN_VEHICLE WHERE POLICY_ID = :PolicyId";

            using var connection = CreateConnection();
            return await connection.QueryAsync<VehicleDto>(sql, new { PolicyId = policyId });
        }

        public async Task<bool> UpdateAsync(VehicleDto dto) // hàm này viết tắt không sử dụng Dynamic, bởi vì không cần lấy Id như Create, chỉ cần tên trong dto truyền vào giống y hệt với sau dấu : :LicensePlate
        {
            var sql = @"UPDATE INSURANCE_USER.DHN_VEHICLE 
                        SET LICENSE_PLATE = :LicensePlate, 
                            BRAND = :Brand, 
                            MODEL = :Model, 
                            YEAR_MANUFACTURED = :YearManufactured 
                        WHERE VEHICLE_ID = :VehicleId";
            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, dto);
            return rowsAffected > 0;
        }
    }
}
