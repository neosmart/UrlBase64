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
        private const PaddingPolicy DefaultPaddingPolicy = PaddingPolicy.Discard;

#if WITH_SPAN
        // Forward mapping from any of 64 binary values to their URL-safe Base64 equivalents
        private readonly static byte[] ToBase64 = new byte[] {
            (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H',
            (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P',
            (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X',
            (byte)'Y', (byte)'Z', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f',
            (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n',
            (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v',
            (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', (byte)'3',
            (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'-', (byte)'_'
        };

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
#else
        private readonly static char[] DoublePadding = new[] { '=', '=' };
#endif

        static UrlBase64()
        {
        }

        public static string Encode(byte[] bytes, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Every 24 bits become 32 bits, including the trailing padding
#if WITH_SPAN
            return Encode(bytes.AsSpan(), padding);
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
        public static string Encode(ReadOnlySpan<byte> input, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            return Encoding.ASCII.GetString(EncodeUtf8(input, padding));
        }

        public static byte[] EncodeUtf8(ReadOnlySpan<byte> input, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Shadow the global static array with a ReadOnlySpan to help the compiler optimize things
            ReadOnlySpan<byte> ToBase64 = UrlBase64.ToBase64.AsSpan();

            // Every three input bytes become 4 output bytes, and there are possibly two bytes of padding
            int maxLength = (input.Length + 2) / 3 * 4;
            Debug.Assert(maxLength == Base64.GetMaxEncodedToUtf8Length(input.Length));
            int paddingLength = (3 - (input.Length % 3)) % 3;
            int length = (padding == PaddingPolicy.Preserve) ? maxLength : maxLength - paddingLength;
            var utf8 = new byte[length];

            // Read three bytes at a time, which give us four bytes of base64-encoded output
            int i = 0;
            int j = 0;
            for (; i + 3 <= input.Length; i += 3)
            {
                int threeBytes = input[i] << 16 | input[i + 1] << 8 | input[i + 2];
                int fourBytes =
                      (ToBase64[(threeBytes >> 0) & 0x3F] << 24)
                    | (ToBase64[(threeBytes >> 6) & 0x3F] << 16)
                    | (ToBase64[(threeBytes >> 12) & 0x3F] << 8)
                    | (ToBase64[threeBytes >> 18]);
                utf8[j++] = (byte)fourBytes;
                utf8[j++] = (byte)(fourBytes >> 8);
                utf8[j++] = (byte)(fourBytes >> 16);
                utf8[j++] = (byte)(fourBytes >> 24);
            }

            // Handle the remaining bytes not divisible by 3
            if (i < input.Length)
            {
                switch (input.Length - i)
                {
                    case 2:
                        {
                            int num = (input[i] << 16) | (input[i + 1] << 8);
                            i += 2;
                            var bytes = (ToBase64[(num >> 6) & 0x3F] << 16)
                                | (ToBase64[(num >> 12) & 0x3F] << 8)
                                | ToBase64[num >> 18];
                            utf8[j++] = (byte)(bytes >> 0);
                            utf8[j++] = (byte)(bytes >> 8);
                            utf8[j++] = (byte)(bytes >> 16);
                            break;
                        }
                    case 1:
                        {
                            int oneByte = input[i++] << 8;
                            var bytes = (ToBase64[(oneByte >> 4) & 0x3F] << 8) | ToBase64[oneByte >> 10];
                            utf8[j++] = (byte)bytes;
                            utf8[j++] = (byte)(bytes >> 8);
                            break;
                        }
                }

                // Add padding if necessary
                if (padding == PaddingPolicy.Preserve)
                {
                    for (int k = 0; k < paddingLength; k++)
                    {
                        utf8[j++] = (byte)'=';
                    }
                }
            }
            Debug.Assert(i == input.Length);

            return utf8;
        }

        public static byte[] Decode(ReadOnlySpan<char> input)
        {
            // Shadow the global static array with a ReadOnlySpan to help the compiler optimize things
            ReadOnlySpan<byte> FromBase64 = UrlBase64.FromBase64.AsSpan();

            // Simplify our calculations by always using unpadded UrlBase64 input:
            input = input.TrimEnd('=');
            int unpaddedLength = input.Length;
            int paddingLength = ((4 - input.Length & 0b11) & 0b11);
            Debug.Assert(paddingLength == (4 - input.Length % 4) % 4);
            // Padding length *could* be 3 here, but that's actually an invalid input we'll throw on below
            int paddedLength = unpaddedLength + paddingLength;
            int maxDecodedLength = ((paddedLength + 4 - 1) >> 2) * 3;
            Debug.Assert(maxDecodedLength == ((int)Math.Ceiling(paddedLength / 4.0)) * 3);
            Debug.Assert(Base64.GetMaxDecodedFromUtf8Length(paddedLength) == maxDecodedLength);
            int decodedLength = maxDecodedLength - paddingLength;
            var decoded = new byte[decodedLength];
            // Unrolled read of 4 characters
            int i = 0, j = 0;
            for (; i + 4 <= input.Length; i += 4)
            {
                // Every eight bits are actually six bits
                int bytes =
                    (FromBase64[input[i]] << 18) |
                    (FromBase64[input[i + 1]] << 12) |
                    (FromBase64[input[i + 2]] << 6) |
                    (FromBase64[input[i + 3]]);
                decoded[j++] = (byte)(bytes >> 16);
                decoded[j++] = (byte)(bytes >> 8);
                decoded[j++] = (byte)(bytes);
                Debug.Assert(j <= decodedLength);
            }

            // Handle left-over bits in case of input that should have had padding
            if (i < input.Length)
            {
                var bytes = (input.Length - i) switch
                {
                    3 => (FromBase64[input[i]] << 18) | (FromBase64[input[i + 1]] << 12) | (FromBase64[input[i + 2]] << 6) | 0xFF,
                    2 => (FromBase64[input[i]] << 18) | (FromBase64[input[i + 1]] << 12) | (0xFF << 6) | 0xFF,
                    _ => throw new InvalidOperationException($"Invalid input provided. {nameof(input)}.Length % 4 can never be less than 2, even without padding."),
                };

                if (paddingLength == 2)
                {
                    decoded[j++] = (byte)(bytes >> 16);
                }
                else if (paddingLength == 1)
                {
                    decoded[j++] = (byte)(bytes >> 16);
                    decoded[j++] = (byte)(bytes >> 8);
                }
                else
                {
                    decoded[j++] = (byte)(bytes >> 16);
                    decoded[j++] = (byte)(bytes >> 8);
                    decoded[j++] = (byte)(bytes >> 0);
                }
                Debug.Assert(j <= decodedLength);
            }

            return decoded;
        }
#endif
    }
}
