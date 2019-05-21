using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WebApiFileUpload.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = @"D:\Temp\ubuntu-18.04-desktop-amd64 .iso";
            var postUrl = "http://localhost:64745/api/Upload";
            UpLoadFile(file, postUrl);
            Console.ReadLine();
        }

        const long BYTES_PER_CHUNK = 1000 * 1024;
        private static void UpLoadFile(string file, string postUrl)
        {
            if (!File.Exists(file))
                return;
            FileInfo fileInfo = new FileInfo(file);
            var size = fileInfo.Length;
            long start = 0;
            var end = BYTES_PER_CHUNK;
            long completed = 0;
            var fail = 0;
            var count = size % BYTES_PER_CHUNK == 0 ? size / BYTES_PER_CHUNK : (size / BYTES_PER_CHUNK) + 1;
            var fileMD5 = GetMD5Hash(file);
            while (start < size)
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        using (var content = new MultipartFormDataContent())
                        {
                            var bytes = GetData(file, start);
                            var fileContent = new ByteArrayContent(bytes);
                            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = Path.GetFileName(file) };
                            var data = new
                            {
                                Total = count,
                                Index = completed,
                                FileName = Path.GetFileName(file),
                                FileMD5 = fileMD5,
                                ByteMD5 = GetMD5Hash(bytes)
                            };
                            var json = JsonConvert.SerializeObject(data);
                            content.Add(new StringContent(json.ToString(), Encoding.UTF8, "application/json"));
                            content.Add(fileContent);
                            Console.WriteLine($"\n========{(completed + 1).ToString()}========");
                            Console.WriteLine($" {(completed + 1)}/{count}");
                            var response = client.PostAsync(postUrl, content).Result;
                            if (response != null)
                            {
                                if (response.IsSuccessStatusCode == true)
                                {
                                    var responseString = response.Content.ReadAsStringAsync().Result;
                                    var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                                    object progress = string.Empty;
                                    object msg = string.Empty;
                                    object code = string.Empty;
                                    info.TryGetValue("progress", out progress);
                                    info.TryGetValue("msg", out msg);
                                    info.TryGetValue("code", out code);
                                    var completedbyte = size / 100 * decimal.Parse(progress.ToString());
                                    switch (code.ToString())
                                    {
                                        case "200"://文件流写入成功
                                            Console.WriteLine($"{msg} 文件大小{CountSize(size)} 已完成{CountSize((long)completedbyte)} 上传进度 {progress}% ");
                                            completed = completed + 1;
                                            break;
                                        case "302"://文件已经存在
                                            if (decimal.Parse(progress.ToString()) == 100)
                                                completed = count;
                                            Console.WriteLine($"{msg}");
                                            break;
                                        case "400": //文件流校验失败
                                            Console.WriteLine($"{msg} 文件大小{CountSize(size)} 已完成{CountSize((long)completedbyte)} 上传进度 {progress}%");
                                            fail++;
                                            break;
                                        case "500"://错误
                                            Console.WriteLine($"{msg} 文件大小{CountSize(size)} 已完成{CountSize((long)completedbyte)} 上传进度 {progress}%");
                                            fail++;
                                            break;
                                    }
                                }
                                if (completed == count)
                                    break;
                                if (fail > 5)
                                {
                                    Console.WriteLine($"{file}文件上传失败超过5次，退出上传！");
                                    break;
                                }
                            }
                            start = end;
                            end = start + BYTES_PER_CHUNK;
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
        }
        private static byte[] GetData(string filePath, long position)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                long left = fs.Length;
                var bytes = new byte[] { };
                if ((left - position) - BYTES_PER_CHUNK > 0)
                    bytes = new byte[BYTES_PER_CHUNK];
                else
                    bytes = new byte[left - position];
                int maxLength = bytes.Length;
                fs.Position = position;
                int num = 0;
                if (left < maxLength)
                    num = fs.Read(bytes, 0, Convert.ToInt32(left));
                else
                    num = fs.Read(bytes, 0, maxLength);
                return bytes;
            }
        }
        private static string CountSize(long Size)
        {
            string m_strSize = "";
            long FactSize = 0;
            FactSize = Size;
            if (FactSize < 1024.00)
                m_strSize = FactSize.ToString("F2") + " Byte";
            else if (FactSize >= 1024.00 && FactSize < 1048576)
                m_strSize = (FactSize / 1024.00).ToString("F2") + " K";
            else if (FactSize >= 1048576 && FactSize < 1073741824)
                m_strSize = (FactSize / 1024.00 / 1024.00).ToString("F2") + " M";
            else if (FactSize >= 1073741824)
                m_strSize = (FactSize / 1024.00 / 1024.00 / 1024.00).ToString("F2") + " G";
            return m_strSize;
        }
        private static string GetMD5Hash(byte[] bytedata)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytedata);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }
        private static string GetMD5Hash(string fileName)
        {
            if (!File.Exists(fileName))
                return null;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
                }
            }
        }
    }
}
