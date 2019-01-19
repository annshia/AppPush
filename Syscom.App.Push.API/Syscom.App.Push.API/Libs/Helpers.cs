using System;
using System.Security.Cryptography;
using System.Text;

namespace Syscom.App.Push.API.Libs
{
    public static class Helpers
    {
        public static string ToMD5Hash(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            using (var md5 = MD5.Create())
            {
                return ByteArrayToString(md5.ComputeHash(bytes), 4);
            }
        }

        private static string ByteArrayToString(byte[] ba, int delimiter)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            int count = 0;
            foreach (byte b in ba)
            {
                hex.AppendFormat("{0:x2}", b);
                count++;
                if (count % delimiter == 0)
                {
                    //hex.Append(" ");
                }
            }
            return hex.ToString().ToUpper().Trim();
        }
    }
}
