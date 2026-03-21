using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

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
        public async Task<PolicyCreatedEvent> AddClaimAsync(ClaimCreateDto dto)
        {
            // Bỏ phần tính MAX(ID) đi, để Oracle tự lo
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
            INSERT INTO INSURANCE_USER.DHN_CLAIM 
            (POLICY_ID, CLAIM_DATE, AMOUNT_CLAIMED, STATUS, AMOUNT_APPROVED, DESCRIPTION)
            VALUES (:PolicyId, :ClaimDate, :AmountClaimed, 'Approved', :AmountClaimed, :Description)
            RETURNING CLAIM_ID INTO :newId";

                var parameters = new DynamicParameters();
                parameters.Add("PolicyId", dto.PolicyId);
                parameters.Add("ClaimDate", dto.ClaimDate);
                parameters.Add("AmountClaimed", dto.AmountClaimed);
                parameters.Add("Description", dto.Description);
                parameters.Add("newId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(sql, parameters);

                // Trả về "Snapshot" mới nhất của toàn bộ Hợp đồng
                return await EnrichPolicyData(connection, dto.PolicyId, "UPDATE");
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

        public async Task<int> GetPolicyIdByClaimId(int claimId)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Truy vấn đơn giản để lấy ID hợp đồng từ bảng bồi thường
                var sql = "SELECT POLICY_ID FROM INSURANCE_USER.DHN_CLAIM WHERE CLAIM_ID = :claimId";
                return await connection.QuerySingleAsync<int>(sql, new { claimId });
            }
        }

        // 1. Hàm PUBLIC - Dùng để Service gọi từ bên ngoài
        public async Task<PolicyCreatedEvent> EnrichPolicyData(int id, string action)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Truyền biến 'connection' vào hàm xử lý lõi
                return await EnrichPolicyData(connection, id, action);
            }
        }


        // 2. Hàm PRIVATE - Chứa logic SQL, dùng nội bộ trong Repository
        private async Task<PolicyCreatedEvent> EnrichPolicyData(OracleConnection connection, int id, string action)
        {
            connection.BindByName = true;

            // 1. SQL Policy: Phải JOIN để lấy được Tên khách hàng, Tên đại lý, Tên loại bảo hiểm
            string sqlPolicy = @"
        SELECT p.POLICY_ID as PolicyId, 
               p.POLICY_NUMBER as PolicyNumber, 
               p.PREMIUM_AMOUNT as PremiumAmount,
               p.START_DATE as StartDate,
               p.END_DATE as EndDate,
               p.STATUS as Status,
               p.CUSTOMER_ID as CustomerId,
               p.AGENT_ID as AgentId,
               p.INS_TYPE_ID as InsTypeId,
               cust.FULL_NAME as CustomerName,  -- Cần JOIN để có cột này
               age.FULL_NAME as AgentName,     -- Cần JOIN để có cột này
               typ.TYPE_NAME as InsTypeName,   -- Cần JOIN để có cột này
               v.BRAND, 
               v.MODEL 
                FROM INSURANCE_USER.DHN_POLICY p 
                INNER JOIN INSURANCE_USER.DHN_CUSTOMER cust ON p.CUSTOMER_ID = cust.CUSTOMER_ID
                INNER JOIN INSURANCE_USER.DHN_AGENT age ON p.AGENT_ID = age.AGENT_ID
                INNER JOIN INSURANCE_USER.DHN_INSURANCE_TYPE typ ON p.INS_TYPE_ID = typ.INS_TYPE_ID
                LEFT JOIN INSURANCE_USER.DHN_VEHICLE v ON p.POLICY_ID = v.POLICY_ID 
                WHERE p.POLICY_ID = :id";

                    // 2. SQL Claims: Lấy danh sách các hồ sơ bồi thường đã duyệt
                    string sqlClaims = @"
                SELECT CLAIM_ID as ClaimId, 
                       AMOUNT_APPROVED as AmountApproved, 
                       STATUS 
                FROM INSURANCE_USER.DHN_CLAIM 
                WHERE POLICY_ID = :id AND STATUS = 'Approved'";

            // Thực thi query chính
            var rawData = await connection.QuerySingleOrDefaultAsync<dynamic>(sqlPolicy, new { id });
            if (rawData == null) return null;

            // 3. Mapping dữ liệu (Dùng Convert để an toàn với kiểu dữ liệu Oracle)
            var eventData = new PolicyCreatedEvent
            {
                PolicyId = Convert.ToInt32(rawData.POLICYID),
                PolicyNumber = rawData.POLICYNUMBER?.ToString(),
                CustomerId = Convert.ToInt32(rawData.CUSTOMERID),
                AgentId = Convert.ToInt32(rawData.AGENTID),
                InsTypeId = Convert.ToInt32(rawData.INSTYPEID),

                // Gán các trường Tên đã JOIN ở trên
                CustomerName = rawData.CUSTOMERNAME?.ToString(),
                AgentName = rawData.AGENTNAME?.ToString(),
                InsTypeName = rawData.INSTYPENAME?.ToString(),

                PremiumAmount = Convert.ToDecimal(rawData.PREMIUMAMOUNT),
                StartDate = Convert.ToDateTime(rawData.STARTDATE),
                EndDate = Convert.ToDateTime(rawData.ENDDATE),
                Status = rawData.STATUS?.ToString(),
                Action = action,

                Vehicle = new VehicleInfo
                {
                    Brand = rawData.BRAND?.ToString(),
                    Model = rawData.MODEL?.ToString()
                }
            };

            // 4. Lấy mảng Claims
            var claims = await connection.QueryAsync<ClaimInfo>(sqlClaims, new { id });
            eventData.Claims = claims.ToList();

            return eventData;
        }
    }
}
