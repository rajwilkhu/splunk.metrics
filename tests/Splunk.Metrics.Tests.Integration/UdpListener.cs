﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Splunk.Metrics.Tests.Integration
{   
    public class UdpListener : IDisposable 
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly List<byte[]> _receivedBytes = new List<byte[]> ();
        private readonly UdpClient _udpClient;
        private readonly IPAddress _localIpAddress = IPAddress.Parse("127.0.0.1");
        private readonly ManualResetEventSlim _writtenEvent = new ManualResetEventSlim();
        
        public UdpListener(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            Port = Ports.GetFreePort();
            var uEndpoint = new IPEndPoint(_localIpAddress, Port);
            _udpClient = new UdpClient(new IPEndPoint(_localIpAddress, Port));
            _udpClient.BeginReceive (RxCallback, new UdpState(_udpClient, uEndpoint));
        }

        public int Port { get; }
        
        private void RxCallback(IAsyncResult result)
        {
            var udpClient = ((UdpState)result.AsyncState).Client;
            var ipEndpoint = ((UdpState)result.AsyncState).Endpoint;
            
            var receivedBytes = udpClient.EndReceive (result, ref ipEndpoint);
            _receivedBytes.Add (receivedBytes);
            
            _testOutput.WriteLine("Received Bytes ___________________________");
            _testOutput.WriteLine(receivedBytes.ToString ());
            _writtenEvent.Set();
        }

        private class UdpState
        {
            public UdpClient Client { get; }
            public IPEndPoint Endpoint { get; }

            public UdpState(UdpClient udpClient, IPEndPoint ipEndpoint)
            {
                Client = udpClient;
                Endpoint = ipEndpoint;
            }
        }
        
        public void Dispose() => _udpClient?.Dispose();

        public string GetWrittenBytesAsString()
        {
            _writtenEvent.Wait(2000);
            return Encoding.UTF8.GetString(_receivedBytes.SelectMany(bArray => bArray).ToArray());
        }
    }
}