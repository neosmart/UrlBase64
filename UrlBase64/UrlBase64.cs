using System;
#if WITH_SPAN
using System.Buffers.Text;
using System.Diagnostics;
using System.Text;
#else
using System.Collections.Generic;
#endif

namespace NeoSmart.Utils
{
    public static class UrlBase64
    {
        private readonly static char[] DoublePadding = new[] { '=', '=' };
        private const PaddingPolicy DefaultPaddingPolicy = PaddingPolicy.Discard;

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

        public static string Encode(byte[] bytes, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Every 24 bits become 32 bits, including the trailing padding
#if WITH_SPAN
            return Encoding.ASCII.GetString(Encode(bytes.AsSpan(), padding));
#else
            var encoded = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');
            if (padding == PaddingPolicy.Discard)
            {
                encoded = encoded.TrimEnd('=');
            }

            return encoded;
#endif
        }

        public static byte[] Decode(string encoded)
        {
#if WITH_SPAN
            return Decode(encoded.AsSpan());
#else
            var chars = new List<char>(encoded.Length + 2);
            chars.AddRange(encoded.ToCharArray());

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
#endif
        }

#if WITH_SPAN
        public static byte[] Encode(ReadOnlySpan<byte> input, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Every three input bytes become 4 output bytes, and there are possibly two bytes of padding
            int length = (input.Length + 2) / 3 * 4;
            Debug.Assert(length == Base64.GetMaxEncodedToUtf8Length(input.Length));
            Span<byte> utf8 = stackalloc byte[length];
            Base64.EncodeToUtf8(input, utf8, out var bytesRead, out var bytesWritten, isFinalBlock: true);
            Debug.Assert(length == bytesWritten);
            Debug.Assert(bytesRead == input.Length);

            // .Replace('+', '-').Replace('/', '_')
            for (int i = length - 1; i >= 0; --i)
            {
                utf8[i] = utf8[i] switch
                {
                    (byte)'+' => (byte)'-',
                    (byte)'/' => (byte)'_',
                    byte b => b,
                };
            }

            if (padding == PaddingPolicy.Discard)
            {
                // Max padding in standard base64 is two trailing equal signs
                if (utf8.Length > 1 && utf8[utf8.Length - 2] == (byte)'=')
                {
                    return utf8.Slice(0, utf8.Length - 2).ToArray();
                }
                else if (utf8.Length > 0 && utf8[utf8.Length - 1] == (byte)'=')
                {
                    return utf8.Slice(0, utf8.Length - 1).ToArray();
                }
            }

            return utf8.ToArray();
        }

        public static byte[] Decode(ReadOnlySpan<char> input)
        {
            unsafe
            {
                // Every four letters represent three bytes, but there could be two bytes of padding missing
                // int decodedLength = ((int)Math.Ceiling(input.Length / 4.0)) * 3;
                int decodedLength = ((input.Length + 4 - 1) >> 2) * 3;
                Debug.Assert(decodedLength == ((int)Math.Ceiling(input.Length / 4.0)) * 3);
                // Debug.Assert(Base64.GetMaxDecodedFromUtf8Length(input.Length) == decodedLength);
                var decodedPtr = stackalloc byte[decodedLength];

                // Unrolled read of 4 characters
                int i = 0, j = 0;
                for (; i + 4 <= input.Length; i += 4)
                {
                    // Every eight bits are actually six bits
                    uint bytes =
                        (((uint)FromBase64[input[i]]) << 18) |
                        (((uint)FromBase64[input[i + 1]]) << 12) |
                        (((uint)FromBase64[input[i + 2]]) << 6) |
                        (FromBase64[input[i + 3]]);
                    decodedPtr[j++] = (byte)(bytes >> 16);
                    decodedPtr[j++] = (byte)(bytes >> 8);
                    decodedPtr[j++] = (byte)(bytes);
                    Debug.Assert(j <= decodedLength);
                }

                // Handle left-over bits in case of missing input padding
                int manualPaddingBytes = input.Length > 1 && input[input.Length - 2] == '=' ? 2
                    : input.Length > 0 && input[input.Length - 1] == '=' ? 1 : 0;
                if (i < input.Length)
                {
                    var (paddingBytes, bytes) = (input.Length - i) switch
                    {
                        3 => (1, (((uint)FromBase64[input[i]]) << 18) | (((uint)FromBase64[input[i + 1]]) << 12) | (((uint)FromBase64[input[i + 2]]) << 6) | 0xFF),
                        2 => (2, (((uint)FromBase64[input[i]]) << 18) | (((uint)FromBase64[input[i + 1]]) << 12) | (((uint)0xFF) << 6) | 0xFF),
                        _ => throw new InvalidOperationException($"Invalid input provided. {nameof(input)}.Length % 4 can never be less than 2, even without padding."),
                    };
                    manualPaddingBytes += paddingBytes;
                    decodedPtr[j++] = (byte)(bytes >> 16);
                    decodedPtr[j++] = (byte)(bytes >> 8);
                    decodedPtr[j++] = (byte)(bytes >> 0);
                    Debug.Assert(j <= decodedLength);
                }

                var decodedSpan = new Span<byte>(decodedPtr, decodedLength);
                // Count trailing padding, if any. Maximum possible padding is two = signs.
                if (manualPaddingBytes == 2)
                {
                    return decodedSpan.Slice(0, decodedLength - 2).ToArray();
                }
                else if (manualPaddingBytes == 1)
                {
                    return decodedSpan.Slice(0, decodedLength - 1).ToArray();
                }

                return decodedSpan.ToArray();
            }
        }
#endif
    }
}
