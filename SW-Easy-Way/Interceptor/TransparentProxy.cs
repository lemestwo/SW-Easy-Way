using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Linq;
using TrotiNet;

namespace SW_Easy_Way.Interceptor
{
	internal class TransparentProxy : BaseProxy
	{
		private Uri _requestUri;

		public TransparentProxy(HttpSocket clientSocket) : base(clientSocket) { }

		public static TransparentProxy CreateProxy(HttpSocket clientSocket)
		{
			return new TransparentProxy(clientSocket);
		}

		protected override void OnReceiveRequest(HttpRequestLine req)
		{
			_requestUri = req.Uri;
			/*var method = req.Method.ToUpper();
			if ((method == "POST" || method == "PUT" || method == "PATCH") && req.Uri.AbsoluteUri.Contains("api/gateway_c2"))
			{
				var content = Encoding.ASCII.GetString(SocketBP.Buffer, 0, Array.IndexOf(SocketBP.Buffer, (byte)0));
				content = content.Substring(content.IndexOf("\r\n\r\n", StringComparison.Ordinal));

				JObject json = JObject.Parse(Proxy.DecryptRequest(content));
				Debug.WriteLine(json["command"]);
			}*/
		}

		protected override void OnReceiveResponse()
		{
			if (ResponseStatusLine.StatusCode != HttpStatus.OK || !ResponseHeaders.Headers.ContainsKey("content-type")) return;
			if (RequestLine.Method != "POST" || !_requestUri.AbsoluteUri.Contains("api/gateway_c2")) return;

			var response = GetContent();
			State.NextStep = null;

			string content;
			using (var sr = new StreamReader(GetResponseMessageStream(response)))
			{
				content = sr.ReadToEnd();
			}

			SendResponseStatusAndHeaders();
			SocketBP.TunnelDataTo(TunnelBP, response);

			if (SocketBP != null)
			{
				SocketBP.CloseSocket();
				SocketBP = null;
			}
			if (SocketPS != null)
			{
				SocketPS.CloseSocket();
				SocketPS = null;
			}
			State.bPersistConnectionBP = false;
			State.bPersistConnectionPS = false;

			var stringResponse = Decrypt.DecryptResponse(content);
			var json = JObject.Parse(stringResponse);
			MainWindow.Instance.HandleNewPacket(json);

			// Temp. saving all commands content to file
			using (var file = new StreamWriter($@"D:/SW-Commands/{json["command"].ToString()}.txt"))
			{
				file.WriteLine(json);
				file.Close();
			}
			Debug.WriteLine($"Proxy Command: {json["command"].ToString()}");
			Debug.WriteLine($"ts: {json["ts_val"].ToString()} / {Ut3()}");
		}

		public static long Ut3()
		{
			DateTime convertedTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"));
			if (convertedTime.IsDaylightSavingTime()) convertedTime = convertedTime.AddHours(-1);
			var time = new DateTimeOffset(convertedTime).ToUnixTimeSeconds();
			var t1 = time - 1519318315;

			BigInteger variation = 22222;
			var v1 = 1000 * t1;
			var v2 = BigInteger.Multiply(BigInteger.Parse("274877907"), variation) >> 38;
			var v3 = BigInteger.Multiply(BigInteger.Parse("274877907"), variation) >> 63;
			var t2 = v1 + (v2 + v3);
			Debug.WriteLine(t2);

			BigInteger f1 = BigInteger.Divide(t2 - 243712668, BigInteger.Pow(2, 3)) * 2361183241434822607;
			BigInteger f2 = BigInteger.Divide(BigInteger.Divide(f1, BigInteger.Pow(2, 64)), BigInteger.Pow(2, 4));
			BigInteger f3 = 1519562027 + f2 ^ 472843912;
			var tsFinal = (long)f3;
			return tsFinal;
		}
	}
}
