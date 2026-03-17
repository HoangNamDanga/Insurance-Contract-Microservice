using OracleSQLCore.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Interface
{
    public interface IClaimDocumentRepository
    {
        /// <summary>
        /// Ghi mới vào Oracle để lấy ID chính thức từ Sequence/Trigger
        /// </summary>
        Task<int> CreateDocumentAsync(ClaimDocumentDto document);

        /// <summary>
        /// Cập nhật thông tin file trong Oracle (nếu cần sửa metadata)
        /// </summary>
        Task<bool> UpdateDocumentAsync(ClaimDocumentDto document);

        /// <summary>
        /// Xóa bản ghi gốc trong Oracle trước khi xóa file vật lý và Mongo
        /// </summary>
        Task<bool> DeleteDocumentAsync(int docId);


        Task<ClaimDocumentDto?> GetDocumentByIdAsync(int docId);
    }
}
