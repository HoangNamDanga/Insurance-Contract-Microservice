using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class AgentRepository : IAgentRepository
    {
        private readonly string _connectionString;

        public AgentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public AgentDto Create(AgentDto agent)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            using var cmd = new OracleCommand("DHN_AGENT_PKG.CREATE_AGENT", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            // Các tham số cũ giữ nguyên
            cmd.Parameters.Add("P_FULL_NAME", OracleDbType.Varchar2).Value = agent.FullName;
            cmd.Parameters.Add("P_PHONE", OracleDbType.Varchar2).Value = agent.Phone;
            cmd.Parameters.Add("P_EMAIL", OracleDbType.Varchar2).Value = agent.Email;
            cmd.Parameters.Add("P_BRANCH_ID", OracleDbType.Int32).Value = agent.BranchId;
            cmd.Parameters.Add("P_HIRE_DATE", OracleDbType.Date).Value = agent.HireDate;

            // THÊM THAM SỐ ĐỂ NHẬN ID TỪ ORACLE
            var outIdParam = new OracleParameter("P_NEW_ID", OracleDbType.Int32)
            {
                Direction = System.Data.ParameterDirection.Output
            };
            cmd.Parameters.Add(outIdParam);

            cmd.ExecuteNonQuery();

            // Lấy ID thực tế gán vào object agent trước khi gửi sang RabbitMQ/MongoDB
            if (outIdParam.Value != null && outIdParam.Value.ToString() != "null")
            {
                agent.AgentId = int.Parse(outIdParam.Value.ToString());
            }

            tran.Commit();
            return agent; // Bây giờ agent.AgentId sẽ mang giá trị 1, 2, 3... thay vì 0
        }

        public void Delete(AgentDto agent)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var tran = conn.BeginTransaction();
            using var cmd = new OracleCommand("DHN_AGENT_PKG.DELETE_AGENT", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            cmd.Parameters.Add("P_AGENT_ID", OracleDbType.Int32).Value = agent.AgentId;
            cmd.ExecuteNonQuery();

            tran.Commit();
        }

        public List<AgentDto> GetAll()
        {
            var result = new List<AgentDto>();
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var cmd = new OracleCommand("DHN_AGENT_PKG.GET_ALL_AGENT", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.Add("P_RESULT", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new AgentDto
                {
                    AgentId = Convert.ToInt32(reader["AGENT_ID"]),
                    FullName = reader["FULL_NAME"].ToString(),
                    Phone = reader["PHONE"]?.ToString(),
                    Email = reader["EMAIL"]?.ToString(),
                    BranchId = reader["BRANCH_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["BRANCH_ID"]),
                    HireDate = reader["HIRE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["HIRE_DATE"])
                });
            }
            return result;
        }

        public AgentDto GetById(int id)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var cmd = new OracleCommand("DHN_AGENT_PKG.GET_AGENT_BY_ID", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
            };

            cmd.Parameters.Add("P_AGENT_ID", OracleDbType.Int32).Value = id;
            cmd.Parameters.Add("P_RESULT", OracleDbType.RefCursor).Direction= System.Data.ParameterDirection.Output;

            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return null;
            return new AgentDto
            {
                AgentId = Convert.ToInt32(reader["AGENT_ID"]),
                FullName = reader["FULL_NAME"].ToString(),
                Phone = reader["PHONE"]?.ToString(),
                Email = reader["EMAIL"]?.ToString(),
                BranchId = reader["BRANCH_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["BRANCH_ID"]),
                HireDate = reader["HIRE_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["HIRE_DATE"])
            };
        }

        public void Update(AgentDto agent)
        {
            using var conn = new OracleConnection(_connectionString);

            conn.Open();

            using var tran = conn.BeginTransaction();
            using var cmd = new OracleCommand("DHN_AGENT_PKG.UPDATE_AGENT", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            cmd.Parameters.Add("P_AGENT_ID", OracleDbType.Int32).Value = agent.AgentId;
            cmd.Parameters.Add("P_FULL_NAME", OracleDbType.Varchar2).Value = agent.FullName;
            cmd.Parameters.Add("P_PHONE", OracleDbType.Varchar2).Value = agent.Phone;
            cmd.Parameters.Add("P_EMAIL", OracleDbType.Varchar2).Value = agent.Email;
            cmd.Parameters.Add("P_BRANCH_ID", OracleDbType.Int32).Value = agent.BranchId;
            cmd.Parameters.Add("P_HIRE_DATE", OracleDbType.Date).Value = agent.HireDate;

            cmd.ExecuteNonQuery();

            tran.Commit();
        }
    }
}
