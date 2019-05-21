using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace WebApiFileUpload.API.Models
{
    public static class Comm
    {
        public static string GetMD5Hash(byte[] bytedata)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(bytedata);
                return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            }
        }

        public static string GetMD5Hash(string fileName)
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