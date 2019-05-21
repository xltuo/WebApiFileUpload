using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApiFileUpload.API.Models
{
    public class FileUploadInfo
    {
        public int Total { get; set; }
        public int Index { get; set; }
        public string FileName { get; set; }
        public string FileMD5 { get; set; }
        public string ByteMD5 { get; set; }
    }
}