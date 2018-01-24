using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text;
using NeoSmart.Utils;
using System;
using System.Collections.Generic;

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
        public void VariableLengthTest()
        {
            //use a fixed seed so we can have deterministic results
            var rng = new Random(0);

            for (int i = 0; i < 256; ++i)
            {
                var array = new byte[i];
                rng.NextBytes(array);

                try
                {
                    var encoded = UrlBase64.Encode(array);
                    var decoded = UrlBase64.Decode(encoded);

                    Assert.IsTrue(array.SequenceEqual(decoded), $"Decoded value mismatch for input of length {i}!");
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
    }
}
