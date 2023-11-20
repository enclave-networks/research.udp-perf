using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Enclave.UdpPerf.Test
{
    class Program
    {
        private const int DefaultPacketSize = 1380; 
        private static int _packetSize;

        private static Dictionary<SocketAddress, EndPoint> _endpointLookup = new Dictionary<SocketAddress, EndPoint>(new SocketAddressContentsComparer());
        private static IPEndPoint _endpointFactory = new IPEndPoint(IPAddress.Any, 0);

        static async Task Main(string[] args)
        {
            var clientOption = new Option<string?>(name: "-c", "Act as a client; destination IP address");
            var packetSizeOption = new Option<int>(name: "-p", () => DefaultPacketSize, "Bytes sent per-packet");

            var rootCommand = new RootCommand
            {
                clientOption,
                packetSizeOption
            };

            rootCommand.SetHandler(async context =>
            {
                string? clientOptionValue = context.ParseResult.GetValueForOption(clientOption);
                int packetSizeOptionValue = context.ParseResult.GetValueForOption(packetSizeOption);
                var cancelToken = context.GetCancellationToken();

                _packetSize = packetSizeOptionValue;

                await RunAsync(clientOptionValue, cancelToken);
            });

            await rootCommand.InvokeAsync(args);
        }

        private static async Task RunAsync(string? destinationIp,CancellationToken cancelToken)
        {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // Discard our socket when the user cancels.
            using var cancelReg = cancelToken.Register(() => udpSocket.Dispose());

            var throughput = new ThroughputCounter();

            // Start a background task to print throughput periodically.
            _ = PrintThroughputAsync(throughput, cancelToken);

            // Client or server?
            if (IPAddress.TryParse(destinationIp, out var destination))
            {
                // Client.                
                Console.WriteLine($"Sending to {destination}:9999");
                await DoSendAsync(udpSocket, new IPEndPoint(destination, 9999), throughput, cancelToken);                
            }
            else
            {
                // Server.
                udpSocket.Bind(new IPEndPoint(IPAddress.Any, 9999));

                Console.WriteLine("Listening on 0.0.0.0:9999");
                Console.WriteLine("Run with -c <ip address> to be a client.");
                await DoReceiveAsync(udpSocket, throughput, cancelToken);
            }
        }

        private static async Task PrintThroughputAsync(ThroughputCounter counter, CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancelToken);

                var count = counter.SampleAndReset();

                var megabytes = count / 1024d / 1024d;

                double pps = count / _packetSize;

                Console.WriteLine("{0:0.00}MBps ({1:0.00}Mbps) - {2:0.00}pps", megabytes, megabytes * 8, pps);
            }
        }

        private static async Task DoSendAsync(Socket udpSocket, IPEndPoint destination, ThroughputCounter throughput, CancellationToken cancelToken)
        {
            // Taking advantage of pre-pinned memory here using the .NET 5 POH (pinned object heap).            
            byte[] buffer = GC.AllocateArray<byte>(_packetSize, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();

            // Put something approaching meaningful data in the buffer.
            for (var idx = 0; idx < _packetSize; idx++)
            {
                bufferMem.Span[idx] = (byte)idx;
            }

            while (!cancelToken.IsCancellationRequested)
            {
                await udpSocket.SendToAsync(bufferMem, SocketFlags.None, destination, cancelToken);

                throughput.Add(bufferMem.Length);
            }
        }

        private static async Task DoReceiveAsync(Socket udpSocket, ThroughputCounter throughput, CancellationToken cancelToken)
        {
            // Taking advantage of pre-pinned memory here using the .NET5 POH (pinned object heap).
            byte[] buffer = GC.AllocateArray<byte>(length: 65527, pinned: true);
            Memory<byte> bufferMem = buffer.AsMemory();
            var receivedAddress = new SocketAddress(udpSocket.AddressFamily);

            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    var receivedBytes = await udpSocket.ReceiveFromAsync(bufferMem, SocketFlags.None, receivedAddress);

                    throughput.Add(receivedBytes);

                    // Update the packet size based on each packet we receive.
                    _packetSize = receivedBytes;

                    var endpoint = GetEndPoint(receivedAddress);

                    // Do something with the endpoint and received data.
                }
                catch (SocketException)
                {
                    // Socket exception means we are finished.
                    break;
                }
            }
        }

        private static EndPoint GetEndPoint(SocketAddress receivedAddress)
        {
            if (!_endpointLookup.TryGetValue(receivedAddress, out var endpoint))
            {
                // Create an EndPoint from the SocketAddress
                endpoint = _endpointFactory.Create(receivedAddress);
                _endpointLookup[receivedAddress] = endpoint;
            }

            return endpoint;
        }

        private class SocketAddressContentsComparer : IEqualityComparer<SocketAddress>
        {
            public bool Equals(SocketAddress? x, SocketAddress? y)
            {
                if (x is null)
                {
                    return y is null;
                }

                if (y is null)
                {
                    return x is null;
                }

                return x.Buffer.Span.SequenceEqual(y.Buffer.Span);
            }

            public int GetHashCode([DisallowNull] SocketAddress obj)
            {
                var hash = new HashCode();
                hash.AddBytes(obj.Buffer.Span);

                return hash.ToHashCode();
            }
        }
    }
}
