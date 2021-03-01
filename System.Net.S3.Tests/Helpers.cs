using System;
using System.IO;
using System.Net.S3;
using System.Threading.Tasks;

namespace System.Net.S3.Tests
{
    internal class Helpers
    {
        internal static void RunContentStreamGenerator(int sizeInKB, Stream stream)
        {
            byte[] data = new byte[1024];
            Random rng = new Random();
            Task.Run(() =>
            {
                for (int i = 0; i < sizeInKB; i++)
                {
                    rng.NextBytes(data);
                    stream.Write(data, 0, data.Length);
                }
                stream.Close();
            });
        }
    }
}