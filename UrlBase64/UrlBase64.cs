using System;
#if WITH_SPAN
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        // Forward mapping from any of 64 binary values to their URL-safe Base64 equivalents.
        // In our dictionary, - takes the place of + and _ takes the place of /.
        private readonly static ReadOnlyMemory<byte> ToBase64 = new byte[] {
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
        private readonly static ReadOnlyMemory<byte> FromBase64 = new byte[] {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0x3E, 0xFF, 0x3E, 0xFF, 0x3F,
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

        /// <summary>
        /// Encodes a binary <paramref name="input"/> to a URL-safe base64 representation, with a configurable
        /// <see cref="PaddingPolicy"/>. <br/>
        /// Use one of the other <code>Encode()</code> overloads if you'll ultimately be using the result
        /// as binary or UTF-8 to save on allocations.
        /// </summary>
        /// <param name="input">The raw, unencoded binary input to transform to base64</param>
        /// <param name="padding">The padding policy, which dictates whether trailing <c>=</c> bytes are
        /// appended to the output, defaulting to <see cref="PaddingPolicy.Discard"/> (i.e. not appended).</param>
        /// <returns>An ASCII (UTF8-compatible) string being the URL-safe base64 representation of the provided <paramref name="input"/>.</returns>
#if WITH_SPAN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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

        /// <summary>
        /// Decodes an input encoded in the url-safe base64 variant to its binary equivalent.
        /// </summary>
        /// <param name="input">The input to be decoded</param>
        /// <returns>A newly allocated array containing the decoded binary result</returns>
#if WITH_SPAN
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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
        /// <summary>
        /// Encodes a binary <paramref name="input"/> to a URL-safe base64 representation, with a configurable
        /// <see cref="PaddingPolicy"/>.<br/>
        /// To encode to bytes instead of a string, see <see cref="EncodeUtf8(ReadOnlySpan{byte}, PaddingPolicy)"/>
        /// or one of the other <c>Encode()</c> overloads.
        /// </summary>
        /// <param name="input">The raw, unencoded binary input to transform to base64</param>
        /// <param name="padding">The padding policy, which dictates whether trailing <c>=</c> bytes are
        /// appended to the output, defaulting to <see cref="PaddingPolicy.Discard"/> (i.e. not appended).</param>
        /// <returns>An ASCII (UTF8-compatible) string being the URL-safe base64 representation of the provided <paramref name="input"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Encode(ReadOnlySpan<byte> input, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            return Encoding.ASCII.GetString(EncodeUtf8(input, padding));
        }

        /// <summary>
        /// Returns the maximum length of the base64-encoded representation of an input of length
        /// <paramref name="inputLength"/>. This can be used to correctly size a buffer for use with
        /// <see cref="Encode(ReadOnlySpan{byte}, Span{byte}, PaddingPolicy)"/>.
        /// </summary>
        /// <param name="inputLength">The length of the unencoded binary input</param>
        /// <returns>The maximum length of the base64 equivalent of input of the provided length</returns>
        /// <exception cref="ArgumentOutOfRangeException">The provided <paramref name="inputLength"/> exceeds the max supported size</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxEncodedLength(int inputLength)
        {
            long maxLength = (inputLength + 2) / 3 * 4;
            if (maxLength > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(inputLength), "Encoded length exceeds supported limits!");
            }
            Debug.Assert(maxLength == Base64.GetMaxEncodedToUtf8Length(inputLength));
            return (int)maxLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEncodedLength(int inputLength, PaddingPolicy padding)
        {
            int paddingLength = (3 - (inputLength % 3)) % 3;
            int maxLength = GetMaxEncodedLength(inputLength);
            int length = (padding == PaddingPolicy.Preserve) ? maxLength : maxLength - paddingLength;
            return length;
        }

        /// <summary>
        /// Encodes the provided binary input <paramref name="input"/> to URL-safe Base64, returning the binary
        /// result as a newly allocated <c>byte[]</c>. See <see cref="Encode(byte[], PaddingPolicy)"/> to get the
        /// result as a string instead. (Getting the result as a <see cref="byte[]"/> performs better.)
        /// </summary>
        /// <param name="input">The binary input to be encoded</param>
        /// <param name="padding">The <see cref="PaddingPolicy"/> to use, specifying whether trailing <c>=</c>
        /// bytes will be appended to the output in some cases.</param>
        /// <returns>A <see cref="byte[]"/> containing the base64-encoded result
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="input"/> length exceeds the max supported size</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] EncodeUtf8(ReadOnlySpan<byte> input, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Every three input bytes become 4 output bytes, and there are possibly two bytes of padding
            var length = GetEncodedLength(input.Length, padding);
            var base64 = new byte[length];
            InnerEncode(input, base64);
            return base64;
        }

        /// <summary>
        /// Encodes the provided binary input <paramref name="input"/> to URL-safe Base64, allocating the resulting
        /// base64-encoded <see cref="Span{byte}"/> from the provided <paramref name="buffer"/> instead of allocating.<br/>
        /// <paramref name="buffer"/>
        /// must be large enough to hold the encoded result; <see cref="GetMaxEncodedLength(int)"/> can be used to
        /// ensure a big enough buffer is used.
        /// </summary>
        /// <param name="input">The binary input to be encoded</param>
        /// <param name="buffer">The buffer to from which the base64-encoded result will be allocated. Do not
        /// read the result from here - use the return value instead!</param>
        /// <param name="padding">The <see cref="PaddingPolicy"/> to use, specifying whether trailing <c>=</c>
        /// bytes will be appended to the output in some cases.</param>
        /// <returns>A <see cref="Span{byte}"/> containing the base64-encoded result, allocated out of
        /// the provided <paramref name="buffer"/>.</returns>
        /// <exception cref="ArgumentException">The provided buffer is too small for the encoded result.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="input"/> length exceeds the max supported size</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Encode(ReadOnlySpan<byte> input, Span<byte> buffer, PaddingPolicy padding = DefaultPaddingPolicy)
        {
            // Every three input bytes become 4 output bytes, and there are possibly two bytes of padding
            var length = GetEncodedLength(input.Length, padding);
            if (buffer.Length < length)
            {
                throw new ArgumentException("Output buffer is not sufficiently long for encoded result!");
            }
            var base64 = buffer.Slice(0, length);
            InnerEncode(input, base64);
            return base64;
        }

        /// <summary>
        /// Encodes directly from <paramref name="input"/> to <paramref name="base64"/>, assuming that
        /// <paramref name="base64"/> is exactly sized to the required output length (accounting for the
        /// padding policy).
        /// </summary>
        /// <param name="input"></param>
        /// <param name="base64"></param>
        public static void InnerEncode(ReadOnlySpan<byte> input, Span<byte> base64)
        {
            // Shadow the global static array with a ReadOnlySpan to help the compiler optimize things
            ReadOnlySpan<byte> ToBase64 = UrlBase64.ToBase64.Span;

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
                base64[j++] = (byte)fourBytes;
                base64[j++] = (byte)(fourBytes >> 8);
                base64[j++] = (byte)(fourBytes >> 16);
                base64[j++] = (byte)(fourBytes >> 24);
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
                            base64[j++] = (byte)(bytes >> 0);
                            base64[j++] = (byte)(bytes >> 8);
                            base64[j++] = (byte)(bytes >> 16);
                            break;
                        }
                    case 1:
                        {
                            int oneByte = input[i++] << 8;
                            var bytes = (ToBase64[(oneByte >> 4) & 0x3F] << 8) | ToBase64[oneByte >> 10];
                            base64[j++] = (byte)bytes;
                            base64[j++] = (byte)(bytes >> 8);
                            break;
                        }
                }

                // Add padding if necessary
                while (j < base64.Length)
                {
                    base64[j++] = (byte)'=';
                }
            }
            Debug.Assert(i == input.Length);
            Debug.Assert(j == base64.Length);
        }

        /// <summary>
        /// Returns the maximum length of the decoded version of a url-safe base64-encoded input of length
        /// <paramref name="encodedLength"/>. This can be used to correctly size a buffer for use with
        /// <see cref="Decode(ReadOnlySpan{byte}, Span{byte})"/>.
        /// </summary>
        /// <param name="encodedLength">The length of the base64-encoded input</param>
        /// <returns>The maximum length of the decoded binary equivalent of input of the provided length</returns>
        /// <exception cref="ArgumentOutOfRangeException">The provided <paramref name="encodedLength"/> was invalid.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetMaxDecodedLength(int encodedLength)
        {
            if (encodedLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(encodedLength), "Invalid encoded input length!");
            }
            return (encodedLength >> 2) * 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int DecodedLength, int PaddingLength) CalculateDecodeLength(int trimmedInputLength)
        {
            int unpaddedLength = trimmedInputLength;
            int paddingLength = ((4 - unpaddedLength & 0b11) & 0b11);
            Debug.Assert(paddingLength == (4 - unpaddedLength % 4) % 4);
            // Padding length *could* be 3 here, but that's actually an invalid input we'll throw in the decode loop
            int paddedLength = unpaddedLength + paddingLength;
            int maxDecodedLength = ((paddedLength + 4 - 1) >> 2) * 3;
            Debug.Assert(maxDecodedLength == ((int)Math.Ceiling(paddedLength / 4.0)) * 3);
            Debug.Assert(Base64.GetMaxDecodedFromUtf8Length(paddedLength) == maxDecodedLength);
            int decodedLength = maxDecodedLength - paddingLength;
            return (decodedLength, paddingLength);
        }

        /// <summary>
        /// Decodes an input encoded in the url-safe base64 variant to its binary equivalent.
        /// </summary>
        /// <param name="input">The input to be decoded</param>
        /// <returns>A newly allocated array containing the decoded binary result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decode(ReadOnlySpan<char> input)
        {
            // Simplify our calculations by always using unpadded UrlBase64 input:
            input = input.TrimEnd('=');
            var (decodedLength, paddingLength) = CalculateDecodeLength(input.Length);
            var decoded = new byte[decodedLength];
            var decodedSpan = DecodeInner(input, decoded, paddingLength);
            Debug.Assert(decodedSpan.Length == decoded.Length);
            return decoded;
        }

        /// <summary>
        /// Decodes ASCII/UTF-8 binary input encoded in the url-safe base64 variant to its binary equivalent.
        /// </summary>
        /// <param name="input">The input to be decoded</param>
        /// <returns>A newly allocated array containing the decoded binary result</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] Decode(ReadOnlySpan<byte> input)
        {
            // Simplify our calculations by always using unpadded UrlBase64 input:
            input = input.TrimEnd('=');
            var (decodedLength, paddingLength) = CalculateDecodeLength(input.Length);
            var decoded = new byte[decodedLength];
            var decodedSpan = DecodeInner(input, decoded, paddingLength);
            Debug.Assert(decodedSpan.Length == decoded.Length);
            return decoded;
        }

        /// <summary>
        /// Decodes an input encoded in the url-safe base64 variant to its binary equivalent, without any
        /// allocations. The result uses storage provided by the <paramref name="buffer"/> parameter.
        /// </summary>
        /// <param name="input">The input to be decoded</param>
        /// <param name="buffer">The span to be used as the backing storage for the decoded result.<br/>
        /// The result must not be read from this parameter; use the return value instead!</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The provided <paramref name="buffer"/> was
        /// not large enough to contain the result.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Decode(ReadOnlySpan<char> input, Span<byte> buffer)
        {
            // Simplify our calculations by always using unpadded UrlBase64 input:
            input = input.TrimEnd('=');
            var (decodedLength, paddingLength) = CalculateDecodeLength(input.Length);
            if (buffer.Length < decodedLength)
            {
                throw new ArgumentOutOfRangeException("Provided decode buffer is not large enough!", nameof(buffer));
            }
            var decoded = buffer.Slice(0, decodedLength);
            return DecodeInner(input, decoded, paddingLength);
        }

        /// <summary>
        /// Decodes ASCII/UTF-8 binary input encoded in the url-safe base64 variant to its binary equivalent, without any
        /// allocations. The result uses storage provided by the <paramref name="buffer"/> parameter.
        /// </summary>
        /// <param name="input">The input to be decoded</param>
        /// <param name="buffer">The span to be used as the backing storage for the decoded result.<br/>
        /// The result must not be read from this parameter; use the return value instead!</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException">The provided <paramref name="buffer"/> was
        /// not large enough to contain the result.</exception>
        /// <exception cref="FormatException">The provided input was not a valid Base64/UrlSafeBase64 value"</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Decode(ReadOnlySpan<byte> input, Span<byte> buffer)
        {
            // Simplify our calculations by always using unpadded UrlBase64 input:
            input = input.TrimEnd('=');
            var (decodedLength, paddingLength) = CalculateDecodeLength(input.Length);
            if (buffer.Length < decodedLength)
            {
                throw new ArgumentOutOfRangeException("Provided decode buffer is not large enough!", nameof(buffer));
            }
            var decoded = buffer.Slice(0, decodedLength);
            return DecodeInner(input, decoded, paddingLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte FromBase64Safe(int i)
        {
            var b = FromBase64.Span[i];
            return b != 0xFF ? b : throw new FormatException("Invalid base64 input provided!");
        }

        /// <summary>
        /// The core decode loop. Makes assumptions about inputs!
        /// </summary>
        /// <param name="input">The input with all trailing <c>=</c> padding removed</param>
        /// <param name="decoded">The output, sized to fit exactly</param>
        /// <param name="paddingLength">The length of the padding that *would* have been present</param>
        /// <returns></returns>
        /// <exception cref="FormatException">The provided input was not a valid Base64/UrlSafeBase64 value"</exception>
        private static Span<byte> DecodeInner(ReadOnlySpan<char> input, Span<byte> decoded, int paddingLength)
        {
            // Shadow the global static array with a ReadOnlySpan to help the compiler optimize things
            ReadOnlySpan<byte> FromBase64 = UrlBase64.FromBase64.Span;

            var invalid = false;
            // Unrolled read of 4 characters
            int i = 0, j = 0;
            for (; i + 4 <= input.Length; i += 4)
            {
                // Every eight bits are actually six bits
                byte a = FromBase64[input[i]];
                byte b = FromBase64[input[i + 1]];
                byte c = FromBase64[input[i + 2]];
                byte d = FromBase64[input[i + 3]];

                invalid |= a == 0xff | b == 0xff | c == 0xff | d == 0xff;

                int bytes = (a << 18) | (b << 12) | (c << 6) | (d);
                decoded[j++] = (byte)(bytes >> 16);
                decoded[j++] = (byte)(bytes >> 8);
                decoded[j++] = (byte)(bytes);
            }

            if (invalid)
            {
                throw new FormatException("Invalid base64 input provided");
            };

            // Handle left-over bits in case of input that should have had padding
            if (i < input.Length)
            {
                var bytes = (input.Length - i) switch
                {
                    3 => (FromBase64Safe(input[i]) << 18) | (FromBase64Safe(input[i + 1]) << 12) | (FromBase64Safe(input[i + 2]) << 6) | 0xFF,
                    2 => (FromBase64Safe(input[i]) << 18) | (FromBase64Safe(input[i + 1]) << 12) | (0xFF << 6) | 0xFF,
                    _ => throw new FormatException($"Invalid input provided. {nameof(input)}.Length % 4 can never be less than 2, even without padding."),
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
            }

            return decoded;
        }

        // The following DecodeInner implementation is a bit-for-bit identical copy of the one above,
        // but the parameter type has been changed from ReadOnlySpan<char> to ReadOnlySpan<byte>!
        // The only way to avoid duplication would be to use .NET 6+ Generic Math support (this would
        // limit the implementation to only .NET 6, which would be ok, but then we'd need to duplicate
        // the code again anyway to provide a decode loop for lower .NET targets, so what's the point?)
        // and require use to use int.CreateTruncating(...) instead of casting to (int) everywhere!

        /// <summary>
        /// The core decode loop. Makes assumptions about inputs!
        /// </summary>
        /// <param name="input">The input with all trailing <c>=</c> padding removed</param>
        /// <param name="decoded">The output, sized to fit exactly</param>
        /// <param name="paddingLength">The length of the padding that *would* have been present</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Invalid input was provided (truncated or padded)</exception>
        /// <exception cref="FormatException">The provided input was not a valid Base64/UrlSafeBase64 value"</exception>
        private static Span<byte> DecodeInner(ReadOnlySpan<byte> input, Span<byte> decoded, int paddingLength)
        {
            // Shadow the global static array with a ReadOnlySpan to help the compiler optimize things
            ReadOnlySpan<byte> FromBase64 = UrlBase64.FromBase64.Span;

            var invalid = false;
            // Unrolled read of 4 characters
            int i = 0, j = 0;
            for (; i + 4 <= input.Length; i += 4)
            {
                // Every eight bits are actually six bits
                byte a = FromBase64[input[i]];
                byte b = FromBase64[input[i + 1]];
                byte c = FromBase64[input[i + 2]];
                byte d = FromBase64[input[i + 3]];

                invalid |= a == 0xff | b == 0xff | c == 0xff | d == 0xff;

                int bytes = (a << 18) | (b << 12) | (c << 6) | (d);
                decoded[j++] = (byte)(bytes >> 16);
                decoded[j++] = (byte)(bytes >> 8);
                decoded[j++] = (byte)(bytes);
            }

            if (invalid)
            {
                throw new FormatException("Invalid base64 input provided");
            };

            // Handle left-over bits in case of input that should have had padding
            if (i < input.Length)
            {
                var bytes = (input.Length - i) switch
                {
                    3 => (FromBase64Safe(input[i]) << 18) | (FromBase64Safe(input[i + 1]) << 12) | (FromBase64Safe(input[i + 2]) << 6) | 0xFF,
                    2 => (FromBase64Safe(input[i]) << 18) | (FromBase64Safe(input[i + 1]) << 12) | (0xFF << 6) | 0xFF,
                    _ => throw new FormatException($"Invalid input provided. {nameof(input)}.Length % 4 can never be less than 2, even without padding."),
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
            }

            return decoded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span, char trim)
        {
            int i = span.Length - 1;
            for (; i >= 0 && span[i] == trim; --i) ;
            return span.Slice(0, i + 1);
        }
#endif
    }
}
