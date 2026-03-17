using MongoDBCore.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDBCore.Interfaces
{
    public interface IClaimDocumentRepository
    {
        // Lấy thông tin tài liệu theo DocId (ID từ Oracle)
        Task<ClaimDocumentMongo?> GetByIdAsync(int docId);

        // Thêm mới hoặc Cập nhật nếu đã tồn tại
        Task UpsertAsync(ClaimDocumentMongo document);

        // Xóa tài liệu
        Task<bool> DeleteAsync(int docId);

        // Lấy tất cả tài liệu của một hồ sơ bồi thường
        Task<IEnumerable<ClaimDocumentMongo>> GetByClaimIdAsync(int claimId);
    }
}
