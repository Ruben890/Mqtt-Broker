using System.Security.Cryptography;
using System.Text;

namespace Shared.Utils
{
    public static class ToolsUtils
    {
        public static string GenericCode(int longitudCodigo)
        {
            var generadorAleatorio = RandomNumberGenerator.Create();
            byte[] bytesNumeroAleatorio = new byte[longitudCodigo / 2];
            generadorAleatorio.GetBytes(bytesNumeroAleatorio);

            var constructorCodigo = new StringBuilder(longitudCodigo);
            foreach (var b in bytesNumeroAleatorio)
            {
                constructorCodigo.Append(b.ToString("X2"));
            }

            return constructorCodigo.ToString().Substring(0, longitudCodigo);
        }

        public static string TruncateLongString(this string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return str;

            return str.Substring(0, Math.Min(str.Length, maxLength));
        }




    }
}
