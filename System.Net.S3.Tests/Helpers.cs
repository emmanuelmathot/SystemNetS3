using System;
using System.IO;
using System.Net.S3;
using System.Threading.Tasks;

namespace System.Net.S3.Tests
{
    internal class Helpers
    {
        internal static void RunContentStreamGenerator(int sizeInKB, Stream stream, int bufferSize)
        {
            byte[] data = new byte[bufferSize];
            Random rng = new Random();
            int bytesWritten = 0;
            Task.Run(() =>
            {
                while (bytesWritten < sizeInKB * 1024)
                {
                    int l = bytesWritten + data.Length > sizeInKB * 1024 ? sizeInKB * 1024 - bytesWritten : data.Length;
                    rng.NextBytes(data);
                    stream.Write(data, 0, l);
                    bytesWritten += l;
                }
                stream.Close();
            });
        }
    }
}