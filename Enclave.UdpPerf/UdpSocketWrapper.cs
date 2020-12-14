using Microsoft.Extensions.ObjectPool;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Enclave.UdpPerf
{
    public static class UdpSocketExtensions
    {
        private static readonly ObjectPool<AsyncSocketEventArgs> _socketEventPool = ObjectPool.Create<AsyncSocketEventArgs>();

        public static async ValueTask<int> SendToAsync(this Socket socket, EndPoint destination, Memory<byte> data)
        {
            // Get an async argument from the socket event pool.
            var asyncArgs = _socketEventPool.Get();

            try
            {
                return await asyncArgs.SendToAsync(socket, destination, data);
            }
            finally
            {
                _socketEventPool.Return(asyncArgs);
            }
        }

        public static async ValueTask<ReceiveFromResult?> ReceiveFromAsync(this Socket socket, Memory<byte> buffer)
        {
            // Get an async argument from the socket event pool.
            var asyncArgs = _socketEventPool.Get();

            try
            {
                var recvResult = await asyncArgs.ReceiveFromAsync(socket, buffer);

                if (recvResult.Data.Length == 0)
                {
                    // No data received; socket has stopped.
                    return null;
                }

                return recvResult;
            }
            catch (SocketException)
            {
                // Failure to receive means we're done.
                return null;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            finally
            {
                _socketEventPool.Return(asyncArgs);
            }
        }
    }
}
