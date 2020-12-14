using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enclave.UdpPerf
{
    class AsyncSocketEventArgs : SocketAsyncEventArgs
    {
        // This functions as a non-allocating version of TaskCompletionSource.
        private AsyncValueTaskMethodBuilder<int> _taskCompletion;
        private IPEndPoint _blankEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public AsyncSocketEventArgs()
        {
            Completed += TrackedAsyncSocketEventArgs_Completed;
        }

        public ValueTask<int> SendToAsync(Socket socket, EndPoint destination, Memory<byte> buffer)
        {
            PrepareSend(buffer, destination);

            if (!socket.SendToAsync(this))
            {
                // Completed synchronously; mark as such.
                Complete();
            }

            return _taskCompletion.Task;
        }

        public async ValueTask<ReceiveFromResult> ReceiveFromAsync(Socket socket, Memory<byte> buffer)
        {
            PrepareRecv(buffer);

            if (!socket.ReceiveFromAsync(this))
            {
                // Completed synchronously; mark as such.
                Complete();
            }

            await _taskCompletion.Task;

            Debug.Assert(RemoteEndPoint is object, "Remote Endpoint should be set by ReceiveFrom if it succeeded");

            // Resize the buffer for our result.
            var recvdBuffer = buffer.Slice(0, BytesTransferred);

            return new ReceiveFromResult(recvdBuffer, RemoteEndPoint);
        }

        private void PrepareSend(Memory<byte> buffer, EndPoint remoteEndpoint)
        {
            SetBuffer(buffer);
            _taskCompletion = new AsyncValueTaskMethodBuilder<int>();
            RemoteEndPoint = remoteEndpoint;
        }

        private void PrepareRecv(Memory<byte> buffer)
        {
            SetBuffer(buffer);
            _taskCompletion = new AsyncValueTaskMethodBuilder<int>();
            RemoteEndPoint = _blankEndPoint;
        }

        private void Complete()
        {
            if (SocketError == SocketError.Success)
            {
                _taskCompletion.SetResult(BytesTransferred);
            }
            else
            {
                _taskCompletion.SetException(new SocketException((int)SocketError));
            }
        }

        private void TrackedAsyncSocketEventArgs_Completed(object? sender, SocketAsyncEventArgs e)
        {
            Complete();
        }
    }
}
