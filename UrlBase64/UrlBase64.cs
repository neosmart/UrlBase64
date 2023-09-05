using System;
using System.Collections.Generic;
using System.Diagnostics;
#if WITH_SPAN
using System.Buffers.Text;
#endif

namespace NeoSmart.Utils
{
    public static class UrlBase64
    {
        private readonly static char[] DoublePadding = new[] { '=', '=' };

#if WITH_SPAN
        // Reverse mapping of base64 alphabet to numerical value, such that table[(byte)'A'] = 0, ...
        private readonly static byte[] FromBase64 = new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x3E, 0xFF, 0xFF,
            0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B,
            0x3C, 0x3D, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06,
            0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
            0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x17, 0x18, 0x19, 0xFF, 0xFF, 0xFF, 0xFF, 0x3F,
            0xFF, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20,
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30,
            0x31, 0x32, 0x33, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };
#endif

        static UrlBase64()
        {
        }

        public static string Encode(byte[] bytes, PaddingPolicy padding = PaddingPolicy.Discard)
        {
            // Every 24 bits become 32 bits, including the trailing padding
            //var encodedLength = Math.Ceiling(input.Length / 3.0) * 4;
            var encodedLength = (bytes.Length + 3 - 1) / 3 * 4;

            var encoded = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
            if (padding == PaddingPolicy.Discard)
            {
                encoded = encoded.TrimEnd('=');
            }

            return encoded;
        }

        public static byte[] Decode(string encoded)
        {
            var chars = new List<char>(encoded.ToCharArray());

            for (int i = 0; i < chars.Count; ++i)
            {
                if (chars[i] == '_')
                {
                    chars[i] = '/';
                }
                else if (chars[i] == '-')
                {
                    chars[i] = '+';
                }
            }

            switch (encoded.Length % 4)
            {
                case 2:
                    chars.AddRange(DoublePadding);
                    break;
                case 3:
                    chars.Add('=');
                    break;
            }

            var array = chars.ToArray();

            return Convert.FromBase64CharArray(array, 0, array.Length);
        }

#if WITH_SPAN
        public static Memory<byte> Encode(ReadOnlySpan<byte> input, PaddingPolicy padding = PaddingPolicy.Discard)
        {
            int length = Base64.GetMaxEncodedToUtf8Length(input.Length);
            var utf8 = new byte[length];
            Base64.EncodeToUtf8(input, utf8, out var bytesRead, out var bytesWritten, isFinalBlock: true);
            Debug.Assert(length == bytesWritten);
            Debug.Assert(bytesRead == input.Length);

            // .Replace('+', '-').Replace('/', '_')
            for (int i = length - 1; i >= 0; --i)
            {
                if (utf8[i] == (byte)'+')
                {
                    utf8[i] = (byte)'-';
                }
                else if (utf8[i] == (byte)'/')
                {
                    utf8[i] = (byte)'_';
                }
            }

            if (padding == PaddingPolicy.Discard)
            {
                // Max padding in standard base64 is two trailing equal signs
                if (utf8[length - 2] == (byte)'=')
                {
                    return utf8.AsMemory(0, length - 2);
                }
                else if (utf8[length - 1] == (byte)'=')
                {
                    return utf8.AsMemory(0, length - 1);
                }
            }

            return utf8;
        }

        public static Memory<byte> Decode(ReadOnlySpan<char> input)
        {
            // Every four letters represent three bytes
            int decodedLength = (input.Length + 4 - 1) / 4 * 3;
            var decoded = new byte[decodedLength];

            // Unrolled read of 4 characters
            for (int i = 0, j = 0; i + 4 <= input.Length; i += 4)
            {
                // Every eight bits are actually six bits
                uint bytes =
                    (((uint)FromBase64[input[i]]) << 18) |
                    (((uint)FromBase64[input[i + 1]]) << 12) |
                    (((uint)FromBase64[input[i + 2]]) << 6) |
                    (FromBase64[input[i + 3]]);
                decoded[j++] = (byte)(bytes >> 16);
                decoded[j++] = (byte)(bytes >> 8);
                decoded[j++] = (byte)bytes;
            }

            // Count trailing padding, if any.
            // Maximum possible padding is two = signs
            if (input[input.Length - 2] == '=')
            {
                return decoded.AsMemory(0, decodedLength - 2);
            }
            else if (input[input.Length - 1] == '=')
            {
                return decoded.AsMemory(0, decodedLength - 1);
            }

            return decoded;
        }

#endif
    }
}
