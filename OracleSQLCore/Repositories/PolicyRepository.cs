using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly string _connectionString;
        public PolicyRepository(string connectionString)
        {
            _connectionString = connectionString;
        }



        public async Task<PolicyCreatedEvent> CreateAsync(PolicyDto policy)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            var p = new DynamicParameters();
            p.Add("P_POLICY_NUMBER", policy.PolicyNumber);
            p.Add("P_CUSTOMER_ID", policy.CustomerId);
            p.Add("P_AGENT_ID", policy.AgentId);
            p.Add("P_INS_TYPE_ID", policy.InsTypeId);
            p.Add("P_START_DATE", policy.StartDate);
            p.Add("P_END_DATE", policy.EndDate);
            p.Add("P_PREMIUM", policy.PremiumAmount);
            p.Add("P_STATUS", policy.Status);
            p.Add("P_NEW_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await conn.ExecuteAsync("INSURANCE_USER.DHN_POLICY_PKG.CREATE_POLICY", p, commandType: CommandType.StoredProcedure);

            int newId = p.Get<int>("P_NEW_ID");

            // Gọi hàm phụ để lấy dữ liệu đầy đủ
            return await EnrichPolicyData(conn, newId, "CREATE");
        }

        public async Task<PolicyCreatedEvent> DeleteAsync(int id)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            var p = new DynamicParameters();
            p.Add("P_POLICY_ID", id);

            await conn.ExecuteAsync("INSURANCE_USER.DHN_POLICY_PKG.DELETE_POLICY", p, commandType: CommandType.StoredProcedure);

            return new PolicyCreatedEvent { PolicyId = id , Action = "DELETE"};
        }

        public async Task<List<PolicyDto>> GetAllAsync()
        {
            using var conn = new OracleConnection(_connectionString);

            // Liệt kê chi tiết và gán Alias trùng với thuộc tính trong DTO
            var sql = @"SELECT 
                    POLICY_ID AS PolicyId, 
                    POLICY_NUMBER AS PolicyNumber, 
                    CUSTOMER_ID AS CustomerId, 
                    AGENT_ID AS AgentId, 
                    INS_TYPE_ID AS InsTypeId, 
                    START_DATE AS StartDate, 
                    END_DATE AS EndDate, 
                    PREMIUM_AMOUNT AS PremiumAmount, 
                    STATUS AS Status
                FROM INSURANCE_USER.DHN_POLICY 
                ORDER BY POLICY_ID DESC";

            var result = await conn.QueryAsync<PolicyDto>(sql);
            return result.ToList();
        }

        public async Task<PolicyDto> GetByIdAsync(int id)
        {
            using var conn = new OracleConnection(_connectionString);
            var sql = @"SELECT 
                    POLICY_ID as PolicyId, 
                    POLICY_NUMBER as PolicyNumber, 
                    CUSTOMER_ID as CustomerId, 
                    AGENT_ID as AgentId, 
                    INS_TYPE_ID as InsTypeId, 
                    START_DATE as StartDate, 
                    END_DATE as EndDate, 
                    PREMIUM_AMOUNT as PremiumAmount, 
                    STATUS as Status
                FROM INSURANCE_USER.DHN_POLICY 
                WHERE POLICY_ID = :id";

            return await conn.QuerySingleOrDefaultAsync<PolicyDto>(sql, new { id });
        }



        public async Task<PolicyCreatedEvent> UpdateAsync(PolicyDto policy)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            var p = new DynamicParameters();
            p.Add("P_POLICY_ID", policy.PolicyId);
            p.Add("P_END_DATE", policy.EndDate);
            p.Add("P_PREMIUM", policy.PremiumAmount);
            p.Add("P_STATUS", policy.Status);

            await conn.ExecuteAsync("INSURANCE_USER.DHN_POLICY_PKG.UPDATE_POLICY", p, commandType: CommandType.StoredProcedure);

            // Tái sử dụng hàm phụ
            return await EnrichPolicyData(conn, policy.PolicyId, "UPDATE");
        }


        // --- HELPER: Hàm dùng chung để lấy tên Customer/Agent (Enrichment) ---
        private async Task<PolicyCreatedEvent> EnrichPolicyData(OracleConnection conn, int id, string action)
        {
            // Bổ sung p.CUSTOMER_ID, p.AGENT_ID, p.INS_TYPE_ID vào SQL
            string sql = @"
                SELECT p.POLICY_ID as PolicyId, 
                       p.POLICY_NUMBER as PolicyNumber, 
                       p.STATUS as Status, 
                       p.PREMIUM_AMOUNT as PremiumAmount,
                       p.START_DATE as StartDate, 
                       p.END_DATE as EndDate,
                       p.CUSTOMER_ID as CustomerId,    
                       c.FULL_NAME as CustomerName,
                       p.AGENT_ID as AgentId,          
                       a.FULL_NAME as AgentName,
                       p.INS_TYPE_ID as InsTypeId,     
                       t.TYPE_NAME as InsTypeName
                    FROM INSURANCE_USER.DHN_POLICY p
                    JOIN INSURANCE_USER.DHN_CUSTOMER c ON p.CUSTOMER_ID = c.CUSTOMER_ID
                    JOIN INSURANCE_USER.DHN_AGENT a ON p.AGENT_ID = a.AGENT_ID
                    JOIN INSURANCE_USER.DHN_INSURANCE_TYPE t ON p.INS_TYPE_ID = t.INS_TYPE_ID
                    WHERE p.POLICY_ID = :id";

                    // Dapper sẽ tự động map các cột trên vào thuộc tính tương ứng của PolicyCreatedEvent
                    var eventData = await conn.QuerySingleOrDefaultAsync<PolicyCreatedEvent>(sql, new { id });

                    if (eventData != null)
                    {
                        eventData.Action = action;
                    }

            return eventData;
        }


        // ------------------------------- //


        //huy hop dong va ghi log
        public async Task<PolicyChangedEvent> CancelAsync(CancelPolicyDto request)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                var p = new DynamicParameters();
                p.Add("P_POLICY_ID", request.PolicyId);
                p.Add("P_REASON", request.Reason);

                // Gọi procedure Cancel
                await conn.ExecuteAsync("INSURANCE_USER.DHN_POLICY_PKG.CANCEL_POLICY", p, commandType: CommandType.StoredProcedure);

                // Lấy dữ liệu để bắn event (Lúc này Status đã là CANCELLED)
                return await EnrichChangedDate(conn, request.PolicyId, "CANCEL", request.Reason);
            }
            catch (OracleException ex)
            {
                // Bắt lỗi State Machine từ Oracle (Ví dụ: ORA-20002)
                throw new Exception($"Lỗi không thể hủy: {ex.Message}");
            }
        }


        //gia han hop dong va ghi log
        // Oracle đã xử lý State Machine , lấy trạng thái cũ sau đó insert vào bảng theo dõi log 
        public async Task<PolicyChangedEvent> RenewAsync(RenewPolicyDto request)
        {
            using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                var p = new DynamicParameters();
                p.Add("P_POLICY_ID", request.PolicyId);
                p.Add("P_NEW_END_DATE", request.NewEndDate);
                p.Add("P_ADDITIONAL_PREMIUM", request.AdditionalPremium);
                p.Add("P_NOTES", request.Notes);

                // Gọi Procedure Renew đã có logic kiểm tra trạng thái
                await conn.ExecuteAsync("INSURANCE_USER.DHN_POLICY_PKG.RENEW_POLICY", p, commandType: CommandType.StoredProcedure);

                // Lấy lại dữ liệu đã update (EndDate mới, Premium mới) để gửi sang MongoDB
                return await EnrichChangedDate(conn, request.PolicyId, "RENEW", request.Notes);
            }
            catch (OracleException ex)
            {
                // Trả về lỗi nghiệp vụ từ Oracle (Ví dụ: ORA-20003)
                throw new Exception($"Lỗi nghiệp vụ bảo hiểm: {ex.Message}");
            }
        }


        // --- HELPER mới cho PolicyChangedEvent ---
        public async Task<PolicyChangedEvent> EnrichChangedDate(OracleConnection conn, int id, string action, string notes)
        {
            // Câu lệnh SQL này đã rất chuẩn vì lấy cả EndDate và TotalPremium mới nhất
            string sql = @"
            SELECT POLICY_ID as PolicyId, 
                   POLICY_NUMBER as PolicyNumber, 
                   STATUS as Status, 
                   END_DATE as EndDate, 
                   PREMIUM_AMOUNT as TotalPremium
            FROM INSURANCE_USER.DHN_POLICY 
            WHERE POLICY_ID = :id";

            var eventData = await conn.QuerySingleOrDefaultAsync<PolicyChangedEvent>(sql, new { id });

            if (eventData != null)
            {
                eventData.ActionType = action;
                eventData.LastNotes = notes;
                eventData.ChangeDate = DateTime.Now;
            }
            return eventData;
        }

    }

}
