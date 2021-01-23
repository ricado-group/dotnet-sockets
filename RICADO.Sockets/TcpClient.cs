using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.Sockets
{
    public class TcpClient : IDisposable
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
        public int Available => _socket?.Available ?? 0;

        /// <summary>
        /// Gets a Value that indicates whether this <see cref="TcpClient"/> is Connected to the Remote Host as of the last Send or Receive Operation
        /// </summary>
        public bool Connected => _socket?.Connected ?? false;

        /// <summary>
        /// The Underlying Socket Object
        /// </summary>
        public Socket Socket => _disposed ? null : _socket;

        /// <summary>
        /// Gets or Sets a <see cref="bool"/> value that specifies whether this <see cref="TcpClient"/> is using the Nagle Algorithm
        /// </summary>
        public bool NoDelay
        {
            get
            {
                if(_disposed == false && _socket != null)
                {
                    return _socket.NoDelay;
                }

                return false;
            }
            set
            {
                if(_disposed == false && _socket != null)
                {
                    _socket.NoDelay = value;
                }
            }
        }

        /// <summary>
        /// Gets or Sets a Value that specifies whether this <see cref="TcpClient"/> will delay closing the Socket in an attempt to send all pending data
        /// </summary>
        public LingerOption LingerState
        {
            get
            {
                if(_disposed == false && _socket != null)
                {
                    return _socket.LingerState;
                }

                return null;
            }
            set
            {
                if(_disposed == false && _socket != null)
                {
                    _socket.LingerState = value;
                }
            }
        }

        /// <summary>
        /// Use TCP Keep Alives on this <see cref="TcpClient"/> Connection
        /// </summary>
        public bool KeepAliveEnabled
        {
            get
            {
                if (_disposed == false && _socket != null)
                {
                    object value = _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive);

                    if (value != null && value is bool)
                    {
                        return Convert.ToBoolean(value);
                    }
                }

                return false;
            }
            set
            {
                if(_disposed == false && _socket != null)
                {
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, value);
                }
            }
        }

        /// <summary>
        /// The number of Seconds the TCP Connection will wait for a Keep Alive response before sending another Keep Alive probe
        /// </summary>
        public int KeepAliveInternal
        {
            get
            {
                if (_disposed == false && _socket != null)
                {
                    object value = _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval);

                    if (value != null && value is int)
                    {
                        return Convert.ToInt32(value);
                    }
                }

                return 0;
            }
            set
            {
                if (_disposed == false && _socket != null)
                {
                    _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, value);
                }
            }
        }

        /// <summary>
        /// The number of Seconds the TCP connection will remain Alive or Idle before Keep Alive probes are sent to the remote
        /// </summary>
        public int KeepAliveDelay
        {
            get
            {
                if (_disposed == false && _socket != null)
                {
                    object value = _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime);

                    if (value != null && value is int)
                    {
                        return Convert.ToInt32(value);
                    }
                }

                return 0;
            }
            set
            {
                if (_disposed == false && _socket != null)
                {
                    _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, value);
                }
            }
        }

        /// <summary>
        /// The number of TCP Keep Alive probes that will be sent before the connection is terminated
        /// </summary>
        public int KeepAliveRetryCount
        {
            get
            {
                if (_disposed == false && _socket != null)
                {
                    object value = _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount);

                    if (value != null && value is int)
                    {
                        return Convert.ToInt32(value);
                    }
                }

                return 0;
            }
            set
            {
                if (_disposed == false && _socket != null)
                {
                    _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, value);
                }
            }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Create a new <see cref="TcpClient"/>
        /// </summary>
        /// <param name="host">The Name of the Remote Host</param>
        /// <param name="port">The Port Number of the Remote Host</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public TcpClient(string host, int port)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            _remoteHost = host;

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The Port Number specified is outside the valid Range of IPEndPoint.MinPort or IPEndPoint.MaxPort");
            }

            _remotePort = port;

            initializeSocket();
        }

        /// <summary>
        /// Create a new <see cref="TcpClient"/>
        /// </summary>
        /// <param name="address">The IP Address of the Remote Host</param>
        /// <param name="port">The Port Number of the Remote Host</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public TcpClient(IPAddress address, int port)
        {
            if (address == null)
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

        /// <summary>
        /// Create a new <see cref="TcpClient"/> from an Accepted Socket on a TCP Listener
        /// </summary>
        /// <param name="acceptedSocket">The TCP Socket that was Accepted</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        internal TcpClient(Socket acceptedSocket)
        {
            if(acceptedSocket == null)
            {
                throw new ArgumentNullException(nameof(acceptedSocket));
            }

            if(acceptedSocket.LingerState == null)
            {
                acceptedSocket.LingerState = new LingerOption(true, 0);
            }
            else
            {
                if(acceptedSocket.LingerState.Enabled != true || acceptedSocket.LingerState.LingerTime != 0)
                {
                    acceptedSocket.LingerState.Enabled = true;
                    acceptedSocket.LingerState.LingerTime = 0;
                }
            }

            _socket = acceptedSocket;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Release all resources used by the current instance of <see cref="TcpClient"/>
        /// </summary>
        public void Dispose()
        {
            if (_disposed == true)
            {
                return;
            }

            if (_socket != null)
            {
                try
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                finally
                {
                    _socket.Dispose();
                    _socket = null;
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// Connect to the Remote Host
        /// </summary>
        /// <param name="timeout">The Timeout Period in Milliseconds</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes upon a Successful Connection</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public Task ConnectAsync(int timeout, CancellationToken cancellationToken)
        {
            return ConnectAsync(TimeSpan.FromMilliseconds(timeout), cancellationToken);
        }

        /// <summary>
        /// Connect to the Remote Host
        /// </summary>
        /// <param name="timeout">The Timeout Period</param>
        /// <param name="cancellationToken">A Cancellation Token that can be used to signal the Asynchronous Operation should be Cancelled</param>
        /// <returns>A Task that Completes upon a Successful Connection</returns>
        /// <exception cref="System.TimeoutException"></exception>
        public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            throwIfDisposed();

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                await _socket.ConnectAsync(_remoteHost, _remotePort, cancellationToken);
                return;
            }

            using (CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    Task connectTask = _socket.ConnectAsync(_remoteHost, _remotePort, connectCts.Token).AsTask();
                    Task delayTask = Task.Delay(timeout, delayCts.Token);

                    if (connectTask == await Task.WhenAny(connectTask, delayTask))
                    {
                        delayCts.Cancel();

                        try
                        {
                            await delayTask;
                        }
                        catch
                        {
                        }

                        await connectTask;
                    }
                    else
                    {
                        connectCts.Cancel();

                        try
                        {
                            await connectTask;
                        }
                        catch
                        {
                        }

                        await delayTask;

                        throw new TimeoutException("Failed to Connect to the Remote Host '" + _remoteHost.ToString() + ":" + _remotePort.ToString() + "' within the Timeout Period");
                    }
                }
            }
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

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                return await _socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }

            using (CancellationTokenSource sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                using (CancellationTokenSource delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    Task<int> sendTask = _socket.SendAsync(buffer, SocketFlags.None, sendCts.Token).AsTask();
                    Task delayTask = Task.Delay(timeout, delayCts.Token);

                    if (sendTask == await Task.WhenAny(sendTask, delayTask))
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
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _socket.LingerState = new LingerOption(true, 0);
        }

        /// <summary>
        /// Throws an Exception if this <see cref="TcpClient"/> instance has been Disposed
        /// </summary>
        /// <exception cref="System.ObjectDisposedException"></exception>
        private void throwIfDisposed()
        {
            if (_disposed == true)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        #endregion
    }
}
