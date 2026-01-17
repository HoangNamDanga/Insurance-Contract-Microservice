using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly string _connectionString;

        public ClaimRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        //Chú ý khi làm việc vs oracle : phải có tên user chứa bảng đó để gọi dapper VD: INSURANCE_USER
        public async Task<int> AddClaimAsync(ClaimCreateDto dto)
        {
            // Bỏ phần tính MAX(ID) đi, để Oracle tự lo
            var sql = @"
                INSERT INTO INSURANCE_USER.DHN_CLAIM (POLICY_ID, CLAIM_DATE, AMOUNT_CLAIMED, STATUS, DESCRIPTION)
                VALUES (:PolicyId, :ClaimDate, :AmountClaimed, 'Pending', :Description)
                RETURNING CLAIM_ID INTO :newId";

            using (var connection = new OracleConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("PolicyId", dto.PolicyId);
                parameters.Add("ClaimDate", dto.ClaimDate);
                parameters.Add("AmountClaimed", dto.AmountClaimed);
                parameters.Add("Description", dto.Description);

                // Đây là nơi nhận ID về
                parameters.Add("newId", dbType: DbType.Int32, direction: ParameterDirection.Output, size: 38);

                await connection.ExecuteAsync(sql, parameters);

                // Trả về ID thực tế vừa sinh ra
                return parameters.Get<int>("newId");
            }
        }

        //Hàm raw vẽ dữ liệu ... làm giàu dữ liệu để đồng bộ sang mongoDb
        public async Task<ClaimSyncDto> GetClaimForSyncAsync(int claimId)
        {
            var sql = @"
                SELECT 
                    c.CLAIM_ID as ClaimId,
                    c.POLICY_ID as PolicyId,
                    p.POLICY_NUMBER as PolicyNumber,
                    cust.FULL_NAME as CustomerName,
                    c.CLAIM_DATE as ClaimDate,
                    c.AMOUNT_CLAIMED as AmountClaimed,
                    c.STATUS as Status,
                    c.DESCRIPTION as Description
                FROM INSURANCE_USER.DHN_CLAIM c
                INNER JOIN INSURANCE_USER.DHN_POLICY p ON c.POLICY_ID = p.POLICY_ID
                INNER JOIN INSURANCE_USER.DHN_CUSTOMER cust ON p.CUSTOMER_ID = cust.CUSTOMER_ID
                WHERE c.CLAIM_ID = :ClaimId";

            using (var connection = new OracleConnection(_connectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<ClaimSyncDto>(sql, new { ClaimId = claimId });
            }
        }
    }
}
