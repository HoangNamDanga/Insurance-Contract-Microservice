using MassTransit;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class InsuranceTypeRepository : IInsuranceTypeRepository
    {
        private readonly string _connectionString;

        public InsuranceTypeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        public InsuranceTypeDto Create(InsuranceTypeDto dto)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();

            using var cmd = new OracleCommand("PKG_INSURANCE_TYPE.SP_CREATE", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = dto.TypeName;
            cmd.Parameters.Add("d_desc", OracleDbType.Varchar2).Value = dto.Description;
            cmd.Parameters.Add("p_out_id", OracleDbType.Int32).Direction = System.Data.ParameterDirection.Output;

            cmd.ExecuteNonQuery();
            tran.Commit();

            // THÀNH:
            if (cmd.Parameters["p_out_id"].Value is Oracle.ManagedDataAccess.Types.OracleDecimal oracleDecimal)
            {
                dto.InsTypeId = oracleDecimal.ToInt32();
            }
            else
            {
                dto.InsTypeId = Convert.ToInt32(cmd.Parameters["p_out_id"].Value);
            }
            return dto;
        }

        public void Delete(int id)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var tran = conn.BeginTransaction();

            using var cmd = new OracleCommand("PKG.INSURANCE_TYPE.SP_DELETE", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
            cmd.ExecuteNonQuery();
            tran.Commit();
        }

        public List<InsuranceTypeDto> GetAll()
        {
            var list = new List<InsuranceTypeDto>();
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var cmd = new OracleCommand("PKG_INSURANCE_TYPE.SP_GETALL", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
            };

            cmd.Parameters.Add("p_cursor", OracleDbType.RefCursor).Direction = System.Data.ParameterDirection.Output;

            using var reader = cmd.ExecuteReader();
            while(reader.Read())
            {
                list.Add(new InsuranceTypeDto
                {
                    InsTypeId = reader["INS_TYPE_ID"] is DBNull ? 0 : Convert.ToInt32(reader["INS_TYPE_ID"].ToString()),
                    TypeName = reader["TYPE_NAME"].ToString(),
                    Description = reader["DESCRIPTION"].ToString()
                });
            }

            return list;
        }

        public InsuranceTypeDto GetById(int id)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();

            using var cmd = new OracleCommand("PKG.INSURANCE_TYPE.SP_GETBYID", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
            cmd.Parameters.Add("p_type_name", OracleDbType.Varchar2, 100).Direction = System.Data.ParameterDirection.Output;
            cmd.Parameters.Add("p_description", OracleDbType.Varchar2, 200).Direction = System.Data.ParameterDirection.Output;
            cmd.ExecuteNonQuery();

            return new InsuranceTypeDto
            {
                InsTypeId = id,
                TypeName = cmd.Parameters["p_type_name"].Value.ToString(),
                Description = cmd.Parameters["p_description"].Value.ToString()
            };
        }

        public void Update(InsuranceTypeDto dto)
        {
            using var conn = new OracleConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();

            using var cmd = new OracleCommand("PKG.INSURANCE_TYPE.SP_UPDATE", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure,
                Transaction = tran
            };

            cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = dto.InsTypeId;
            cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = dto.TypeName;
            cmd.Parameters.Add("p_desc", OracleDbType.Varchar2).Value = dto.Description;

            cmd.ExecuteNonQuery();
            tran.Commit();
        }

    }
}
