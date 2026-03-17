using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDBCore.Entities.Models;
using MongoDBCore.Interfaces;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MongoClaimDocumentController : ControllerBase
    {
        private readonly IClaimDocumentRepository _repo;

        public MongoClaimDocumentController(IClaimDocumentRepository repo)
        {
            _repo = repo;
        }

        // 1. Lấy thông tin tài liệu theo DocId (Dùng để kiểm tra hoặc Frontend gọi)
        [HttpGet("{docId}")]
        public async Task<IActionResult> GetById(int docId)
        {
            var document = await _repo.GetByIdAsync(docId);
            if (document == null) return NotFound(new { message = "Không tìm thấy tài liệu trong MongoDB" });
            return Ok(document);
        }

        // 2. Lấy tất cả tài liệu của một hồ sơ bồi thường
        [HttpGet("claim/{claimId}")]
        public async Task<IActionResult> GetByClaimId(int claimId)
        {
            var documents = await _repo.GetByClaimIdAsync(claimId);
            return Ok(documents);
        }

        // 3. API để Oracle Service gọi sang đồng bộ (Thêm mới hoặc Cập nhật)
        [HttpPost("sync")]
        public async Task<IActionResult> SyncDocument([FromBody] ClaimDocumentMongo document)
        {
            if (document == null || document.DocId <= 0)
                return BadRequest(new { message = "Dữ liệu đồng bộ không hợp lệ" });

            await _repo.UpsertAsync(document);
            return Ok(new { message = "Đồng bộ (Upsert) sang MongoDB thành công", docId = document.DocId });
        }

        // 4. API để Oracle Service gọi sang khi xóa tài liệu
        [HttpDelete("sync/{docId}")]
        public async Task<IActionResult> DeleteSync(int docId)
        {
            var result = await _repo.DeleteAsync(docId);
            if (result)
                return Ok(new { message = $"Đã xóa tài liệu ID {docId} khỏi MongoDB và Cache" });

            return NotFound(new { message = "Không tìm thấy tài liệu để xóa" });
        }
    }
}
