using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class PolicyBeneficiaryRepository : IPolicyBeneficiaryRepository
    {
        private readonly string _connectionString;

        public PolicyBeneficiaryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new OracleConnection(_connectionString);

        public async Task<int> CreateAsync(PolicyBeneficiaryDto dto)
        {
            using var connection = CreateConnection();

            var checklSql = "SELECT COUNT(1) FROM INSURANCE_USER.DHN_POLICY WHERE POLICY_ID = :PolicyId";
            var exists = await connection.ExecuteScalarAsync<int>(checklSql, new { PolicyId = dto.PolicyId });

            if(exists == 0)
            {
                throw new KeyNotFoundException($"Policy với ID {dto.PolicyId} không tồn tại !");
            }

            var sql = @"BEGIN 
                        INSERT INTO INSURANCE_USER.DHN_POLICY_BENEFICIARY (POLICY_ID, FULL_NAME, RELATIONSHIP, PHONE) 
                        VALUES (:PolicyId, :FullName, :Relationship, :Phone) 
                        RETURNING BENEFICIARY_ID INTO :NewId; 
                    END;";

            var parameters = new DynamicParameters();
            parameters.Add("PolicyId", dto.PolicyId);
            parameters.Add("Fullname", dto.FullName);
            parameters.Add("Relationship", dto.Relationship);
            parameters.Add("Phone", dto.Phone);

            // Tham số Output để hứng giá trị ID
            parameters.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output, size: 10);

            await connection.ExecuteAsync(sql, parameters);

            var resultId = parameters.Get<int>(":NewId");
            return resultId;
        }

        public async Task<bool> DeleteAsync(int beneficiaryId)
        {
            var sql = "DELETE FROM INSURANCE_USER.DHN_POLICY_BENEFICIARY WHERE BENEFICIARY_ID = :Id";
            using var connection = CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = beneficiaryId });
            return rowsAffected > 0;
        }

        public async Task<PolicyBeneficiaryDto> GetByIdAsync(int beneficiaryId)
        {
            var sql = @"SELECT BENEFICIARY_ID as BeneficiaryId, 
                               POLICY_ID as PolicyId, 
                               FULL_NAME as FullName, 
                               RELATIONSHIP, Relationship, 
                               PHONE as Phone 
                        FROM INSURANCE_USER.DHN_POLICY_BENEFICIARY WHERE BENEFICIARY_ID = :Id";
            using var connection = CreateConnection();


            return await connection.QuerySingleOrDefaultAsync(sql, new { Id = beneficiaryId });
        }

        public async Task<IEnumerable<PolicyBeneficiaryDto>> GetByPolicyIdAsync(int policyId)
        {
            var sql = @"SELECT BENEFICIARY_ID as VehicleId, 
                               POLICY_ID as PolicyId, 
                               FULL_NAME as FullName, 
                               RELATIONSHIP, Relationship, 
                               PHONE as Phone 
                        FROM INSURANCE_USER.DHN_POLICY_BENEFICIARY WHERE POLICY_ID = :PolicyId";

            using var connection = CreateConnection();
            return await connection.QueryAsync<PolicyBeneficiaryDto>(sql, new { PolicyId = policyId });
        }

        public async Task<bool> UpdateAsync(PolicyBeneficiaryDto dto)
        {
            // 1. Xóa dấu phẩy sau :Phone
            // 2. Bọc tên cột bằng "" để tránh trùng từ khóa (tùy chọn nhưng nên làm)
            var sql = @"UPDATE INSURANCE_USER.DHN_POLICY_BENEFICIARY 
                SET ""FULL_NAME"" = :FullName, 
                    ""RELATIONSHIP"" = :Relationship, 
                    ""PHONE"" = :Phone
                WHERE ""BENEFICIARY_ID"" = :BeneficiaryId";

            using var connection = CreateConnection();

            // Dapper sẽ tự khớp :FullName với dto.FullName, v.v.
            var rowsAffected = await connection.ExecuteAsync(sql, dto);
            return rowsAffected > 0;
        }
    }
}
