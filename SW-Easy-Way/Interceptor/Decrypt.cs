using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SW_Easy_Way.Interceptor
{
	public static class Decrypt
	{
		private static readonly byte[] Key = { 71, 114, 52, 83, 50, 101, 105, 78, 108, 55, 122, 113, 53, 77, 114, 85 };
		private static readonly byte[] Iv = new byte[16];

		public static string DecryptResponse(string content)
		{
			var byteRes = Convert.FromBase64String(content);
			return ZlibDecompressData(DecryptByte(byteRes, Key, Iv));
		}

		public static string DecryptRequest(string content)
		{
			var byteReq = Convert.FromBase64String(content);
			return Encoding.Default.GetString(DecryptByte(byteReq, Key, Iv));
		}

		private static byte[] DecryptByte(byte[] buff, byte[] key, byte[] iv)
		{
			using (var rm = new RijndaelManaged())
			{
				rm.Padding = PaddingMode.PKCS7;
				rm.Mode = CipherMode.CBC;
				rm.KeySize = key.Length * 8;
				var decryptor = rm.CreateDecryptor(key, iv);
				var memoryStream = new MemoryStream(buff);
				var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
				var output = new byte[buff.Length];
				var readBytes = cryptoStream.Read(output, 0, output.Length);
				return output.Take(readBytes).ToArray();
			}
		}

		private static string ZlibDecompressData(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes, 2, bytes.Length - 2))
			{
				using (var ds = new DeflateStream(ms, CompressionMode.Decompress))
				{
					using (var sr = new StreamReader(ds))
					{
						return sr.ReadToEnd();
					}
				}
			}
		}
	}
}
