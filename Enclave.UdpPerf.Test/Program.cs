using Enclave.UdpPerf;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace udp_perf_test
{
    class Program
    {
        private const int PacketSize = 65355;

        static async Task Main(string[] args)
        {
            using var udpSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            var userExitSource = GetUserConsoleCancellationSource();

            var cancelToken = userExitSource.Token;

            using var cancelReg = cancelToken.Register(() => udpSocket.Dispose());

            if (args.Length > 0 && args[0] == "-c" )
            {
                if (args.Length > 1 && IPAddress.TryParse(args[1], out var destination))
                {
                    Console.WriteLine($"Sending to {destination}:9999");
                    await DoSendAsync(udpSocket, new IPEndPoint(destination, 9999), cancelToken);
                }
                else
                {
                    Console.WriteLine("-c argument requires an IP:port combo.");
                }
            }
            else
            {
                udpSocket.Bind(new IPEndPoint(IPAddress.Any, 9999));

                Console.WriteLine("Listening on 0.0.0.0:9999");
                await DoReceiveAsync(udpSocket, cancelToken);
            }
        }

        private static async Task DoSendAsync(Socket udpSocket, IPEndPoint destination, CancellationToken cancelToken)
        {
            var buffer = GC.AllocateArray<byte>(PacketSize, true);
            var bufferMem = buffer.AsMemory();
            var stopwatch = new Stopwatch();
            long bytesSent = 0;

            stopwatch.Start();

            // Put something approaching meaningful data in the buffer.
            for (var idx = 0; idx < PacketSize; idx++)
            {
                bufferMem.Span[idx] = (byte) idx;
            }

            while (!cancelToken.IsCancellationRequested)
            {
                await udpSocket.SendToAsync(destination, bufferMem);

                bytesSent += PacketSize;

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    Console.WriteLine($"{bytesSent / 1024 / 1024}MB sent");
                    bytesSent = 0;

                    stopwatch.Restart();
                }
            }
        }

        private static async Task DoReceiveAsync(Socket udpSocket, CancellationToken cancelToken)
        {
            var buffer = GC.AllocateArray<byte>(PacketSize, true);
            var bufferMem = buffer.AsMemory();
            var stopwatch = new Stopwatch();
            long bytesRecvd = 0;

            stopwatch.Start();

            while(!cancelToken.IsCancellationRequested)
            {
                var result = await udpSocket.ReceiveFromAsync(bufferMem);

                if (result is null)
                {
                    break;
                }

                bytesRecvd += result.Value.Data.Length;

                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    var bytesPerSec = bytesRecvd / stopwatch.Elapsed.TotalSeconds;

                    Console.WriteLine($"{bytesPerSec / 1024 / 1024}MB recvd");
                    bytesRecvd = 0;

                    stopwatch.Restart();
                }
            }
        }

        private static CancellationTokenSource GetUserConsoleCancellationSource()
        {
            var cancellationSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                cancellationSource.Cancel();
            };

            return cancellationSource;
        }
    }
}
