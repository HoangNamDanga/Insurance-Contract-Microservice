using Dapper;
using Oracle.ManagedDataAccess.Client;
using OracleSQLCore.Interface;
using OracleSQLCore.Models.DTOs;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Repositories
{
    public class ClaimDocumentRepository : IClaimDocumentRepository
    {
        private readonly string _connectionString;

        public ClaimDocumentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> CreateDocumentAsync(ClaimDocumentDto document)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("P_CLAIM_ID", document.ClaimId);
                p.Add("P_FILE_NAME", document.FileName);
                p.Add("P_FILE_PATH", document.FilePath);

                //Tham số OUT để nhận lại ID từ Procedure(Sử dụng Returning Into)
                p.Add("P_NEW_ID", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "INSURANCE_USER.DHN_CLAIM_PKG.PRC_ADD_CLAIM_DOCUMENT",
                p,
                commandType: CommandType.StoredProcedure);

                //Trả về Id chính thức từ Oracle
                return p.Get<int>("P_NEW_ID");
            }
        }

        public async Task<bool> DeleteDocumentAsync(int docId)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("P_DOC_ID", docId);

                await connection.ExecuteAsync(
                    "INSURANCE_USER.DHN_CLAIM_PKG.PRC_DELETE_CLAIM_DOCUMENT",
                    p,
                    commandType: CommandType.StoredProcedure);

                return true;
            }
        }

        public async Task<ClaimDocumentDto?> GetDocumentByIdAsync(int docId)
        {
            // Câu lệnh SQL lấy thông tin cần thiết
            string sql = @"SELECT DOC_ID as DocId, 
                           CLAIM_ID as ClaimId, 
                           FILE_NAME as FileName, 
                           FILE_PATH as FilePath 
                    FROM DHN_CLAIM_DOCUMENT 
                    WHERE DOC_ID = :docId"
            ;

            using (var connection = new OracleConnection(_connectionString))
            {
                // Sử dụng Dapper để query dữ liệu và map vào DTO
                return await connection.QueryFirstOrDefaultAsync<ClaimDocumentDto>(
                    sql,
                    new { docId });
            }
        }

        public async Task<bool> UpdateDocumentAsync(ClaimDocumentDto document)
        {
            using (var connection = new OracleConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("P_DOC_ID", document.DocId);
                p.Add("P_FILE_NAME", document.FileName);
                p.Add("P_FILE_PATH", document.FilePath);

                // Đối với Update/Delete, ta kiểm tra số dòng bị ảnh hưởng (ExecuteAsync trả về int)
                var result = await connection.ExecuteAsync(
                    "INSURANCE_USER.DHN_CLAIM_PKG.PRC_UPDATE_CLAIM_DOCUMENT",
                    p,
                    commandType: CommandType.StoredProcedure);

                return true; // Trong Oracle Procedure thường có COMMIT nên ta trả về true nếu không có Exception
            }
        }
    }
}
