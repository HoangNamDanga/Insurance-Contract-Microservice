using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OracleSQLCore.Models.DTOs;
using OracleSQLCore.Services;

namespace CoNhungNgayMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimDocumentController : ControllerBase
    {
        public IClaimDocumentService _claimDocumentService;

        public ClaimDocumentController(IClaimDocumentService claimDocumentService)
        {
            _claimDocumentService = claimDocumentService;
        }
        [HttpPost("upload/{claimId}")]
        public async Task<IActionResult> Upload(int claimId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "Vui lòng chọn file để tải lên." });

            try
            {
                // Thực hiện lưu Oracle, lưu file vật lý và đồng bộ sang Mongo
                var docId = await _claimDocumentService.UploadDocumentAsync(claimId, file);

                if (docId > 0)
                {
                    return Ok(new
                    {
                        Message = "Tải lên và đồng bộ dữ liệu thành công",
                        DocumentId = docId
                    });
                }

                return BadRequest(new { Message = "Lưu dữ liệu vào Oracle thất bại." });
            }
            catch (Exception ex)
            {
                // Log ex ở đây
                return StatusCode(500, new { Message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPut("update-metadata")]
        public async Task<IActionResult> UpdateMetadata([FromBody] ClaimDocumentDto dto)
        {
            if (dto == null || dto.DocId <= 0)
                return BadRequest(new { Message = "Dữ liệu không hợp lệ." });

            try
            {
                var result = await _claimDocumentService.UpdateDocumentMetadataAsync(dto);
                if (result)
                    return Ok(new { Message = "Cập nhật metadata và đồng bộ thành công." });

                return NotFound(new { Message = "Không tìm thấy tài liệu hoặc cập nhật tại Oracle thất bại." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpDelete("{docId}")]
        public async Task<IActionResult> Delete(int docId)
        {
            try
            {
                var result = await _claimDocumentService.RemoveDocumentAsync(docId);
                if (result)
                    return Ok(new { Message = "Xóa tài liệu và cập nhật đồng bộ thành công." });

                return NotFound(new { Message = "Không tìm thấy tài liệu để xóa hoặc thực thi thất bại." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }
}
