using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NeoSmart.Utils;
using System.IO;
using System.Text;

namespace Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<UrlBase64Benchmark>();
        }
    }

    public class UrlBase64Benchmark
    {
        private byte[] shortInput;
        private string shortInputEncoded;
        private byte[] mediumInput;
        private string mediumInputEncoded;
        private byte[] longInput;
        private string longInputEncoded;
        private byte[] veryLongInput;
        private string veryLongInputEncoded;

        public UrlBase64Benchmark()
        {
            shortInput = Encoding.ASCII.GetBytes("ShortString");
            shortInputEncoded = UrlBase64.Encode(shortInput);
            mediumInput = Encoding.ASCII.GetBytes(new string('M', 100)); // 100 characters
            mediumInputEncoded = UrlBase64.Encode(mediumInput);
            longInput = Encoding.ASCII.GetBytes(new string('L', 1000)); // 1000 characters
            longInputEncoded = UrlBase64.Encode(longInput);
            veryLongInput = File.ReadAllBytes("benchmark-input.txt");
            veryLongInputEncoded = UrlBase64.Encode(veryLongInput);
        }

        /*[Benchmark]
        public string EncodeShortInput() => UrlBase64.Encode(shortInput);

        [Benchmark]
        public string EncodeMediumInput() => UrlBase64.Encode(mediumInput);

        [Benchmark]
        public string EncodeLongInput() => UrlBase64.Encode(longInput);

        [Benchmark]
        public string EncodeVeryLongInput() => UrlBase64.Encode(veryLongInput);*/

        [Benchmark]
        public void DecodeShortInput() => UrlBase64.Decode(shortInputEncoded);

        [Benchmark]
        public void DecodeMediumInput() => UrlBase64.Decode(mediumInputEncoded);

        [Benchmark]
        public void DecodeLongInput() => UrlBase64.Decode(longInputEncoded);

        [Benchmark]
        public void DecodeVeryLongInput() => UrlBase64.Decode(veryLongInputEncoded);
    }
}
