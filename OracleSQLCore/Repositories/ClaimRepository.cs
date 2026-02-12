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

        //3. Nghiệp vụ Hủy yêu cầu (Cancel Claim)
        public async Task<bool> CancelClaimAsync(int claimId, string reason)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_claim_id", claimId, DbType.Int32, ParameterDirection.Input);

            // Sử dụng tham số truyền vào từ Interface
            parameters.Add("p_reason", reason ?? "No reason provided", DbType.String, ParameterDirection.Input);

            parameters.Add("p_out_success", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using (var connection = new OracleConnection(_connectionString))
            {
                // Gọi Store Procedure trong Package
                await connection.ExecuteAsync(
                    "INSURANCE_USER.PKG_CLAIM_MANAGEMENT.PRC_CANCEL_CLAIM",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                int result = parameters.Get<int>("p_out_success");
                return result == 1;
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

        public async Task<decimal> GetTotalClaimedAmountByPolicyIdAsync(int policyId)
        {
            using var conn = new OracleConnection(_connectionString);
            var p = new DynamicParameters();

            //Dau vao
            p.Add("p_policy_id", policyId, DbType.Int32, ParameterDirection.Input);

            //Dau ra
            p.Add("p_total_amount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            await conn.ExecuteAsync(
                "INSURANCE_USER.PKG_CLAIM_MANAGEMENT.PRC_GET_TOTAL_CLAIMED",
                p,
            commandType: CommandType.StoredProcedure);

            return p.Get<decimal>("p_total_amount");
        }


        //Nghiệp vụ Duyệt/Từ chối bồi thường (Approve/Reject)
        public async Task<bool> UpdateClaimStatusAsync(int claimId, string status, decimal? amountApproved, string description)
        {
            var parameters = new DynamicParameters();
            parameters.Add("p_claim_id", claimId, DbType.Int32, ParameterDirection.Input);
            parameters.Add("p_status", status, DbType.String, ParameterDirection.Input);
            parameters.Add("p_amount_approved", amountApproved ?? 0, DbType.Decimal, ParameterDirection.Input);
            parameters.Add("p_description", description, DbType.String, ParameterDirection.Input);
            parameters.Add("p_out_success", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using (var connection = new OracleConnection(_connectionString))
            {
                try
                {
                    await connection.ExecuteAsync(
                        "INSURANCE_USER.PKG_CLAIM_MANAGEMENT.PRC_APPROVE_REJECT_CLAIM",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    int success = parameters.Get<int>("p_out_success");

                    // THAY ĐỔI Ở ĐÂY: Nếu không thành công, hãy chủ động quăng lỗi
                    if (success == 0)
                    {
                        throw new Exception("Loi: Yeu cau boi thuong nay da duoc xu ly truoc do!");
                    }

                    return true;
                }
                catch (OracleException ex)
                {
                    // Trường hợp Oracle bắn lỗi trực tiếp qua RAISE_APPLICATION_ERROR
                    throw new Exception($"Lỗi nghiệp vụ Database: {ex.Message}");
                }
            }
        }
    }
}
