#if NETSTANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace RICADO.Sockets
{
    internal static class SocketExtensions
    {
        public static async Task<Socket> AcceptAsync(this Socket socket, CancellationToken cancellationToken)
        {
            TaskCompletionSource<Socket> tcs = new TaskCompletionSource<Socket>();

            cancellationToken.Register(() => tcs.TrySetCanceled(), false);

            Task<Socket> cancellationTask = tcs.Task;

            Task<Socket> acceptTask = Task.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, null);

            if(acceptTask == await Task.WhenAny(acceptTask, cancellationTask))
            {
                return await acceptTask;
            }

            acceptTask.ContinueWith(_ => acceptTask.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            return await cancellationTask;
        }

        public static async Task ConnectAsync(this Socket socket, string host, int port, CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            cancellationToken.Register(() => tcs.TrySetCanceled(), false);

            Task<int> cancellationTask = tcs.Task;

            Task connectTask = Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null);

            if (connectTask == await Task.WhenAny(connectTask, cancellationTask))
            {
                await connectTask;
                return;
            }

            connectTask.ContinueWith(_ => connectTask.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            await cancellationTask;
        }

        public static async Task<int> SendAsync(this Socket socket, byte[] buffer, SocketFlags flags, CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            cancellationToken.Register(() => tcs.TrySetCanceled(), false);

            Task<int> cancellationTask = tcs.Task;

            Func<AsyncCallback, object, IAsyncResult> asyncCallback = (callback, state) => socket.BeginSend(buffer, 0, buffer.Length, flags, callback, state);

            Task<int> sendTask = Task.Factory.FromAsync(asyncCallback, socket.EndSend, null);

            if (sendTask == await Task.WhenAny(sendTask, cancellationTask))
            {
                return await sendTask;
            }

            sendTask.ContinueWith(_ => sendTask.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            return await cancellationTask;
        }

        public static async Task<int> ReceiveAsync(this Socket socket, byte[] buffer, SocketFlags flags, CancellationToken cancellationToken)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            cancellationToken.Register(() => tcs.TrySetCanceled(), false);

            Task<int> cancellationTask = tcs.Task;

            Func<AsyncCallback, object, IAsyncResult> asyncCallback = (callback, state) => socket.BeginReceive(buffer, 0, buffer.Length, flags, callback, state);

            Task<int> receiveTask = Task.Factory.FromAsync(asyncCallback, socket.EndReceive, null);

            if (receiveTask == await Task.WhenAny(receiveTask, cancellationTask))
            {
                return await receiveTask;
            }

            receiveTask.ContinueWith(_ => receiveTask.Exception, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            return await cancellationTask;
        }
    }
}
#endif