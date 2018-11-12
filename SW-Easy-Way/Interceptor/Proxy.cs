using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using TrotiNet;

namespace SW_Easy_Way.Interceptor
{
	public class Proxy
	{
		public IPAddress IpAddress { get; set; } = IPAddress.Any;
		public int Port { get; set; } = 8080;
		public bool IsRunning { get; set; }

		private readonly TcpServer _proxyServer;

		public Proxy()
		{
			_proxyServer = new TcpServer(Port, false) { BindAddress = IpAddress };
		}

		public void Start()
		{
			_proxyServer.Start(TransparentProxy.CreateProxy);
			_proxyServer.InitListenFinished.WaitOne();

			if (_proxyServer.InitListenException != null)
				throw _proxyServer.InitListenException;

			if (!_proxyServer.IsListening) return;

			IsRunning = true;
			Debug.WriteLine($"Listening on {IpAddress}:{Port}");

			while (IsRunning)
			{
				Thread.Sleep(200);
			}
		}

		public void Stop()
		{
			IsRunning = false;
			_proxyServer.Stop();
		}
	}
}
