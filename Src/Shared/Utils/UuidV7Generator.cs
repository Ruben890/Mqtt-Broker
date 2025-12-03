using System.Security.Cryptography;

namespace Shared.Utils
{
    public static class UuidV7Generator
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public static Guid Create()
        {
            var bytes = new byte[16];

            // Timestamp en milisegundos (48 bits)
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var tsBytes = BitConverter.GetBytes(timestamp);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(tsBytes);

            // Copiar los últimos 6 bytes del timestamp
            Array.Copy(tsBytes, 2, bytes, 0, 6);

            // Generar 10 bytes de entropía criptográficamente fuerte
            Rng.GetBytes(bytes.AsSpan(6, 10));

            // Establecer versión 7 (bits 48–51)
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);

            // Establecer variante RFC 4122 (bits 64–65)
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

            return new Guid(bytes);
        }
    }
}
