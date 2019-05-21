using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Hosting;
using System.Web.Http;
using WebApiFileUpload.API.Models;

namespace WebApiFileUpload.API.Controllers
{
    public class UploadController : ApiController
    {
        /// <summary>
        /// 分段上传
        /// </summary>
        const long SEGMENT_SIZE = 1000 * 1024;
        // POST: api/Upload
        [HttpPost, Route("api/Upload")]
        public async System.Threading.Tasks.Task<HttpResponseMessage> PostAsync()
        {
            FileUploadInfo fileUploadInfo = new FileUploadInfo();
            var filePath = string.Empty;
            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);
                foreach (var item in provider.Contents)
                {
                    if (item.Headers.ContentDisposition.FileName == null)
                    {
                        var info = item.ReadAsStringAsync().Result;
                        fileUploadInfo = JsonConvert.DeserializeObject<FileUploadInfo>(info);
                        var root = HostingEnvironment.MapPath("/Resource");
                        string resourcePath = Path.Combine(root, DateTime.Now.ToString("yyyy-MM-dd"));
                        if (!Directory.Exists(resourcePath))
                            Directory.CreateDirectory(resourcePath);
                        filePath = Path.Combine(resourcePath, fileUploadInfo.FileName);
                        if (fileUploadInfo.Index == 0)
                        {
                            if (Comm.GetMD5Hash(filePath) == fileUploadInfo.FileMD5)
                            {
                                return Result(new
                                {
                                    progress = 100,
                                    msg = "文件已存在",
                                    code = HttpStatusCode.Found,
                                });
                            }
                        }
                    }
                    else
                    {
                        var ms = item.ReadAsStreamAsync().Result;
                        using (var br = new BinaryReader(ms))
                        {
                            if (ms.Length <= 0)
                                break;
                            var data = br.ReadBytes((int)ms.Length);
                            if (Comm.GetMD5Hash(data) != fileUploadInfo.ByteMD5)//校验MD5 
                            {
                                return Result(new
                                {
                                    progress = Math.Round((decimal)fileUploadInfo.Index / (decimal)fileUploadInfo.Total * 100, 2),
                                    msg = "文件流损坏写入失败",
                                    code = HttpStatusCode.BadRequest
                                });
                            }
                            using (var stream = new MemoryStream(data))
                            {
                                using (var filestream = File.Open(filePath, FileMode.OpenOrCreate))
                                {
                                    filestream.Seek((long)fileUploadInfo.Index * SEGMENT_SIZE, SeekOrigin.Begin);
                                    stream.CopyTo(filestream);
                                }
                            }
                        }
                    }
                }
                if (fileUploadInfo.Index + 1 == fileUploadInfo.Total)
                {
                    if (Comm.GetMD5Hash(filePath) == fileUploadInfo.FileMD5)//文件上传完成并且校验MD5
                    {
                        return Result(new
                        {
                            progress = Math.Round((decimal)(fileUploadInfo.Index + 1) / (decimal)fileUploadInfo.Total * 100, 2),
                            msg = "文件上传完成",
                            code = HttpStatusCode.OK,
                        });
                    }
                }

                return Result(new
                {
                    progress = Math.Round((decimal)(fileUploadInfo.Index + 1) / (decimal)fileUploadInfo.Total * 100, 2),
                    msg = "文件流写入成功",
                    code = HttpStatusCode.OK,
                });
            }
            catch (Exception ex)
            {
                return Result(new
                {
                    progress = 0,
                    msg = $"文件流写入失败 {ex.Message}",
                    code = HttpStatusCode.InternalServerError,
                });
            }
        }

        private HttpResponseMessage Result(object resultInfo)
        {
            var result = new HttpResponseMessage()
            {
                Content = new StringContent(JsonConvert.SerializeObject(resultInfo), Encoding.GetEncoding("UTF-8"), "application/json"),
            };
            return result;
        }
    }
}
