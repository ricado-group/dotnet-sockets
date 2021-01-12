using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.Sockets
{
    public class UdpClient : IDisposable
    {
        #region Private Properties

        private Socket _socket = null;
        private bool _disposed = false;

        private string _remoteHost = null;
        private int _remotePort = IPEndPoint.MinPort;

        #endregion


        #region Public Properties

        /// <summary>
        /// The Number of Bytes Available to Read from the Remote Host
        /// </summary>
        public int Available
        {
            get
            {
                if (_socket != null)
                {
                    return _socket.Available;
                }

                return 0;
            }
        }

        /// <summary>
        /// The Underlying Socket Object
        /// </summary>
        public Socket Socket
        {
            get
            {
                return _socket;
            }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Create a new <see cref="UdpClient"/>
        /// </summary>
        /// <param name="host">The Name of the Remote Host</param>
        /// <param name="port">The Port Number of the Remote Host</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public UdpClient(string host, int port)
        {
            if(host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _remoteHost = host;

            if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The Port Number specified is outside the valid Range of IPEndPoint.MinPort or IPEndPoint.MaxPort");
            }

            _remotePort = port;

            initializeSocket();
        }

        /// <summary>
        /// Create a new <see cref="UdpClient"/>
        /// </summary>
        /// <param name="address">The IP Address of the Remote Host</param>
        /// <param name="port">The Port Number of the Remote Host</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public UdpClient(IPAddress address, int port)
        {
            if(address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            _remoteHost = address.ToString();

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The Port Number specified is outside the valid Range of IPEndPoint.MinPort or IPEndPoint.MaxPort");
            }

            _remotePort = port;

            initializeSocket();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Release all resources used by the current instance of <see cref="UdpClient"/>
        /// </summary>
        public void Dispose()
        {
            if(_disposed == true)
            {
                return;
            }
            
            if(_socket != null)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Dispose();
                _socket = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Send Data to the Remote Host
        /// </summary>
        /// <param name="buffer">The Data to Send</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes sent to the Remote Host</returns>
        public Task<int> SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return SendAsync(buffer, Timeout.InfiniteTimeSpan, cancellationToken);
        }

        /// <summary>
        /// Send Data to the Remote Host
        /// </summary>
        /// <param name="buffer">The Data to Send</param>
        /// <param name="timeout">The Timeout Period in Milliseconds</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes sent to the Remote Host</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public Task<int> SendAsync(ReadOnlyMemory<byte> buffer, int timeout, CancellationToken cancellationToken)
        {
            return SendAsync(buffer, TimeSpan.FromMilliseconds(timeout), cancellationToken);
        }

        /// <summary>
        /// Send Data to the Remote Host
        /// </summary>
        /// <param name="buffer">The Data to Send</param>
        /// <param name="timeout">The Timeout Period</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes sent to the Remote Host</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public async Task<int> SendAsync(ReadOnlyMemory<byte> buffer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            throwIfDisposed();

            if(timeout == Timeout.InfiniteTimeSpan)
            {
                return await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }

            using (CancellationTokenSource sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    Task<int> sendTask = _socket.SendAsync(buffer, SocketFlags.None, sendCts.Token).AsTask();
                    Task delayTask = Task.Delay(timeout, delayCts.Token);

                    if(sendTask == await Task.WhenAny(sendTask, delayTask))
                    {
                        delayCts.Cancel();

                        try
                        {
                            await delayTask;
                        }
                        catch
                        {
                        }

                        return await sendTask;
                    }
                    else
                    {
                        sendCts.Cancel();

                        try
                        {
                            await sendTask;
                        }
                        catch
                        {
                        }

                        await delayTask;

                        throw new TimeoutException("Failed to Send to the Remote Host '" + _remoteHost.ToString() + ":" + _remotePort.ToString() + "' within the Timeout Period");
                    }
                }
            }
        }

        /// <summary>
        /// Receive Data from the Remote Host
        /// </summary>
        /// <param name="buffer">The Data Received</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes received to the Remote Host</returns>
        public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return ReceiveAsync(buffer, Timeout.InfiniteTimeSpan, cancellationToken);
        }

        /// <summary>
        /// Receive Data from the Remote Host
        /// </summary>
        /// <param name="buffer">The Data Received</param>
        /// <param name="timeout">The Timeout Period in Milliseconds</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes received to the Remote Host</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public Task<int> ReceiveAsync(Memory<byte> buffer, int timeout, CancellationToken cancellationToken)
        {
            return ReceiveAsync(buffer, TimeSpan.FromMilliseconds(timeout), cancellationToken);
        }

        /// <summary>
        /// Receive Data from the Remote Host
        /// </summary>
        /// <param name="buffer">The Data Received</param>
        /// <param name="timeout">The Timeout Period in Milliseconds</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes with the number of Bytes received to the Remote Host</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public async Task<int> ReceiveAsync(Memory<byte> buffer, TimeSpan timeout, CancellationToken cancellationToken)
        {
            throwIfDisposed();

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
            }

            using (CancellationTokenSource receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    Task<int> receiveTask = _socket.ReceiveAsync(buffer, SocketFlags.None, receiveCts.Token).AsTask();
                    Task delayTask = Task.Delay(timeout, delayCts.Token);

                    if (receiveTask == await Task.WhenAny(receiveTask, delayTask))
                    {
                        delayCts.Cancel();

                        try
                        {
                            await delayTask;
                        }
                        catch
                        {
                        }

                        return await receiveTask;
                    }
                    else
                    {
                        receiveCts.Cancel();

                        try
                        {
                            await receiveTask;
                        }
                        catch
                        {
                        }

                        await delayTask;

                        throw new TimeoutException("Failed to Receive from the Remote Host '" + _remoteHost.ToString() + ":" + _remotePort.ToString() + "' within the Timeout Period");
                    }
                }
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Initializes the Socket
        /// </summary>
        private void initializeSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            _socket.Connect(_remoteHost, _remotePort);
        }

        /// <summary>
        /// Throws an Exception if this <see cref="UdpClient"/> instance has been Disposed
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        private void throwIfDisposed()
        {
            if(_disposed == true)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion
    }
}
