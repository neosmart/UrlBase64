using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;
using NeoSmart.Utils;
using System;
using System.Buffers.Text;
using System.Buffers;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        public const PaddingPolicy DefaultPaddingPolicy = PaddingPolicy.Discard;

        [TestMethod]
        public void BasicTest()
        {
            var foo = Encoding.UTF8.GetBytes("foo");
            var encoded = UrlBase64.Encode(foo);
            var decoded = UrlBase64.Decode(encoded);

            Assert.IsTrue(decoded.SequenceEqual(foo), "Decoded value mismatch!");
        }

        [TestMethod]
        public void EncodeToSpan()
        {
            var foo = Encoding.UTF8.GetBytes("foo");
            var encoded1 = UrlBase64.EncodeUtf8(foo.AsSpan());
            var encoded2 = UrlBase64.Encode(foo);
            var encoding = new UTF8Encoding(false);
            CollectionAssert.AreEqual(encoding.GetBytes(encoded2), encoded1.ToArray());
        }

        [TestMethod]
        public void DecodeFromSpan()
        {
            var encoding = new UTF8Encoding(false);
            var foo = encoding.GetBytes("foo");
            var encoded = UrlBase64.Encode(foo);
            var decoded = UrlBase64.Decode(encoded.AsSpan());
            CollectionAssert.AreEqual(foo, decoded.ToArray());
        }

        [TestMethod]
        public void DecodeFromSpanPadded1()
        {
            var encoding = new UTF8Encoding(false);
            var foo = encoding.GetBytes("foo11");
            var encoded = UrlBase64.Encode(foo, PaddingPolicy.Preserve);
            Assert.AreEqual(Convert.ToBase64String(foo, Base64FormattingOptions.None).Replace('+', '-').Replace('/', '_'), encoded);
            var decoded = UrlBase64.Decode(encoded.AsSpan());
            CollectionAssert.AreEqual(foo, decoded.ToArray());
        }

        [TestMethod]
        public void DecodeFromSpanPadded2()
        {
            var encoding = new UTF8Encoding(false);
            var foo = encoding.GetBytes("foo1");
            var encoded = UrlBase64.Encode(foo, PaddingPolicy.Preserve);
            Assert.AreEqual(Convert.ToBase64String(foo, Base64FormattingOptions.None).Replace('+', '-').Replace('/', '_'), encoded);
            var decoded = UrlBase64.Decode(encoded.AsSpan());
            CollectionAssert.AreEqual(foo, decoded.ToArray());
        }

        [TestMethod]
        public void DecodeFromSpanPadded1InsteadOf2()
        {
            var encoding = new UTF8Encoding(false);
            var foo = encoding.GetBytes("foo1");
            var encoded = UrlBase64.Encode(foo, PaddingPolicy.Preserve);
            Assert.AreEqual(Convert.ToBase64String(foo, Base64FormattingOptions.None).Replace('+', '-').Replace('/', '_'), encoded);
            var decoded = UrlBase64.Decode(encoded.AsSpan(0, encoded.Length - 1));
            CollectionAssert.AreEqual(foo, decoded.ToArray());
        }

        [TestMethod]
        public void VariableLengthTest()
        {
            // Use a fixed seed so we can have deterministic results
            var rng = new Random(0);

            for (int i = 0; i < 256; ++i)
            {
                var array = new byte[i];
                rng.NextBytes(array);

                try
                {
                    var encoded = UrlBase64.Encode(array, PaddingPolicy.Discard);
                    Assert.AreEqual(Convert.ToBase64String(array, Base64FormattingOptions.None).Replace('+', '-').Replace('/', '_').TrimEnd('='), encoded);
                    var decoded = UrlBase64.Decode(encoded);

                    CollectionAssert.AreEqual(array, decoded, $"Decoded value mismatch for input of length {i}! ");

                    encoded = UrlBase64.Encode(array, PaddingPolicy.Preserve);
                    Assert.AreEqual(Convert.ToBase64String(array, Base64FormattingOptions.None).Replace('+', '-').Replace('/', '_'), encoded);
                    decoded = UrlBase64.Decode(encoded);

                    CollectionAssert.AreEqual(array, decoded, $"Decoded value mismatch for input of length {i}! ");
                }
                catch (FormatException ex)
                {
                    throw new Exception($"Decoded value mismatch for input of length {i}!", ex);
                }
            }
        }

        private static byte[] SystemEncodeUtf8(byte[] input)
        {
            var systemEncoded = new byte[UrlBase64.GetMaxEncodedLength(input.Length)];
            var systemResult = Base64.EncodeToUtf8(input, systemEncoded, out var bytesConsumed, out var bytesWritten);
            Assert.AreEqual(OperationStatus.Done, systemResult);
            Assert.AreEqual(input.Length, bytesConsumed);
            Assert.AreEqual(systemEncoded.Length, bytesWritten);
            systemEncoded.AsSpan().Replace((byte)'+', (byte)'-').Replace((byte)'/', (byte)'_');
            return systemEncoded;
        }

        [TestMethod]
        public void Utf8VariableLengthTest()
        {
            // Use a fixed seed so we can have deterministic results
            var rng = new Random(0);

            for (int i = 0; i < 256; ++i)
            {
                var array = new byte[i];
                rng.NextBytes(array);

                try
                {
                    var encoded = UrlBase64.EncodeUtf8(array, PaddingPolicy.Discard);
                    var systemEncoded = SystemEncodeUtf8(array).AsSpan().TrimEnd((byte)'=');
                    Assert.IsTrue(systemEncoded.SequenceEqual(encoded));

                    var decoded = UrlBase64.Decode(encoded);
                    CollectionAssert.AreEqual(array, decoded, $"Decoded value mismatch for input of length {i}! ");

                    systemEncoded = SystemEncodeUtf8(array);
                    encoded = UrlBase64.EncodeUtf8(array, PaddingPolicy.Preserve);
                    Assert.IsTrue(systemEncoded.SequenceEqual(encoded));

                    decoded = UrlBase64.Decode(encoded);
                    CollectionAssert.AreEqual(array, decoded, $"Decoded value mismatch for input of length {i}! ");
                }
                catch (FormatException ex)
                {
                    throw new Exception($"Decoded value mismatch for input of length {i}!", ex);
                }
            }
        }

        [TestMethod]
        public void PaddingPolicyTest()
        {
            var tests = new(string input, string output)[]
            {
                ("1", "MQ=="),
                ("11", "MTE="),
                ("111", "MTEx")
            };

            foreach (var test in tests)
            {
                Assert.AreEqual(test.output, UrlBase64.Encode(Encoding.UTF8.GetBytes(test.input), PaddingPolicy.Preserve), "Did not find expected padding in encoded output!");
                Assert.AreEqual(test.output.TrimEnd('='), UrlBase64.Encode(Encoding.UTF8.GetBytes(test.input)), "Found unexpected padding in encoded output!");
                Assert.AreEqual(UrlBase64.Encode(Encoding.UTF8.GetBytes(test.input)), UrlBase64.Encode(Encoding.UTF8.GetBytes(test.input), DefaultPaddingPolicy), "Default padding policy behavior does not match expected!");
            }
        }

        [TestMethod]
        // This is a regression test for issue #7: https://github.com/neosmart/UrlBase64/issues/7
        public void DecodeNonUrlSafeBase64()
        {
            // Decoding regular base64 input with + instead of -
            var input = "Q29udGFpbnMgYSBwbHVzIMOlw6bDn+KApuKIgsuaxpLiiIY=";
            byte[] expected = new byte[] { 0x43, 0x6F, 0x6E, 0x74, 0x61, 0x69, 0x6E, 0x73, 0x20, 0x61, 0x20, 0x70, 0x6C, 0x75, 0x73, 0x20, 0xC3, 0xA5, 0xC3, 0xA6, 0xC3, 0x9F, 0xE2, 0x80, 0xA6, 0xE2, 0x88, 0x82, 0xCB, 0x9A, 0xC6, 0x92, 0xE2, 0x88, 0x86 };
            var actual = UrlBase64.Decode(input);
            CollectionAssert.AreEqual(expected, actual, "Mismatch in comparison results!");

            // Decoding regular base64 input with / instead of _
            input = "Q29udGFpbnMgYSBzbGFzaCDDuOKImsucw5/iiILiiJrLmsOn4oiC4omkw58=";
            expected = new byte[] { 0x43, 0x6F, 0x6E, 0x74, 0x61, 0x69, 0x6E, 0x73, 0x20, 0x61, 0x20, 0x73, 0x6C, 0x61, 0x73, 0x68, 0x20, 0xC3, 0xB8, 0xE2, 0x88, 0x9A, 0xCB, 0x9C, 0xC3, 0x9F, 0xE2, 0x88, 0x82, 0xE2, 0x88, 0x9A, 0xCB, 0x9A, 0xC3, 0xA7, 0xE2, 0x88, 0x82, 0xE2, 0x89, 0xA4, 0xC3, 0x9F };
            actual = UrlBase64.Decode(input);
            CollectionAssert.AreEqual(expected, actual, "Mismatch in comparison results!");
        }

        [TestMethod]
        public void DecodeInvalid()
        {
            // Make sure these throw...
            Assert.ThrowsException<FormatException>(() => UrlBase64.Decode("invalid!"));
            Assert.ThrowsException<FormatException>(() => UrlBase64.Decode("!!"));

            // ...and this doesn't
            UrlBase64.Decode(UrlBase64.Encode([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]));
        }
    }

    static class SpanExtensions
    {
        public static Span<byte> Replace(this Span<byte> span, byte search, byte replace)
        {
            for (int i = 0; i < span.Length; ++i)
            {
                if (span[i] == search)
                {
                    span[i] = replace;
                }
            }

            return span;
        }

        public static ReadOnlySpan<byte> TrimEnd(this ReadOnlySpan<byte> span, char trim)
        {
            int i = span.Length - 1;
            for (; i >= 0 && span[i] == trim; --i) ;
            return span.Slice(0, i + 1);
        }
    }
}
