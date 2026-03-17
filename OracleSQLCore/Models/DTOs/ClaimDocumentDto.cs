using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSQLCore.Models.DTOs
{
    public class ClaimDocumentDto
    {
        // Khóa chính (Tự sinh từ Trigger/Sequence trong Oracle)
        public int DocId { get; set; }

        // FK liên kết tới hồ sơ bồi thường
        public int ClaimId { get; set; }

        // Tên hiển thị của file (VD: hoa_don_vien_phi.pdf)
        public string FileName { get; set; }

        // Đường dẫn vật lý hoặc URL trên server lưu file
        public string FilePath { get; set; }

        // Ngày giờ tải lên
        public DateTime UploadDate { get; set; }
    }
}
