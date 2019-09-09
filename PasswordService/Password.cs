using System;
using System.Security;
using PasswordService.Properties;

namespace PasswordService
{
    public class Password
    {
        internal static SecureString Get()
        {
            var encrypted = Settings.Default.Password;
            if (string.IsNullOrWhiteSpace(encrypted)) throw new ArgumentException("No password has been set.");

            return StringEx.Decrypt(encrypted);
        }

        internal static string Set(string password)
        {
            var secure = password.ToSecureString();
            var encrypted = StringEx.Encrypt(secure);
            Settings.Default.Password = encrypted;
            Settings.Default.Save();
            return encrypted;
        }
    }
}