using System;
using System.Net;

namespace Enclave.UdpPerf
{
    public readonly struct ReceiveFromResult
    {
        public ReceiveFromResult(Memory<byte> data, EndPoint remoteEndPoint)
        {
            Data = data;
            RemoteEndPoint = remoteEndPoint;
        }

        public Memory<byte> Data { get; }

        public EndPoint RemoteEndPoint { get; }
    }
}
