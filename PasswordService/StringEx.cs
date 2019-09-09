using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace PasswordService
{
    public static class StringEx
    {
        private static readonly byte[] _entropy = Encoding.Unicode.GetBytes(@"3EkC~_#:%,HA");

        public static SecureString Decrypt(string encryptedData)
        {
            try
            {
                var decryptedData = ProtectedData.Unprotect(Convert.FromBase64String(encryptedData), _entropy, DataProtectionScope.CurrentUser);
                return ToSecureString(Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        public static string Encrypt(SecureString input)
        {
            var encryptedData = ProtectedData.Protect(Encoding.Unicode.GetBytes(ToInsecureString(input)), _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static string ToInsecureString(this SecureString input)
        {
            var ptr = Marshal.SecureStringToBSTR(input);
            try
            {
                return Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
        }

        public static SecureString ToSecureString(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var secure = new SecureString();
            foreach (var c in input) secure.AppendChar(c);
            secure.MakeReadOnly();
            return secure;
        }
    }
}