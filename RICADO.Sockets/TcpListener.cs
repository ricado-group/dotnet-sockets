using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.Sockets
{
    public class TcpListener : IDisposable
    {
        #region Private Properties

        private Socket? _socket = null;
        private bool _disposed = false;

        private readonly IPEndPoint _localEndPoint;

        #endregion


        #region Public Properties

        /// <summary>
        /// The Local IP End-Point this <see cref="TcpListener"/> is Bound to
        /// </summary>
        public IPEndPoint LocalEndPoint => _localEndPoint;

        /// <summary>
        /// The Underlying Socket Object
        /// </summary>
        public Socket? Socket => _disposed ? null : _socket;

        /// <summary>
        /// Gets whether this <see cref="TcpListener"/> is Actively Listening for Incoming TCP Connections
        /// </summary>
        public bool IsListening => _disposed ? false : _socket?.IsBound ?? false;

        #endregion


        #region Constructors

        /// <summary>
        /// Create a new <see cref="TcpListener"/>
        /// </summary>
        /// <param name="localEndPoint">The Local <see cref="IPEndPoint"/> to Listen for TCP Connections</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public TcpListener(IPEndPoint localEndPoint)
        {
            if (localEndPoint == null)
            {
                throw new ArgumentNullException(nameof(localEndPoint));
            }

            _localEndPoint = localEndPoint;

            initializeSocket();
        }

        /// <summary>
        /// Create a new <see cref="TcpListener"/>
        /// </summary>
        /// <param name="address">The Local IP Address to Listen for TCP Connections</param>
        /// <param name="port">The Local Port Number to Listen for TCP Connections</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public TcpListener(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(port), "The Local Port Number specified is outside the valid Range of IPEndPoint.MinPort or IPEndPoint.MaxPort");
            }

            _localEndPoint = new IPEndPoint(address, port);

            initializeSocket();
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Release all resources used by the current instance of <see cref="TcpListener"/>
        /// </summary>
        public void Dispose()
        {
            if (_disposed == true)
            {
                return;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Start Listening for Incoming TCP Connections
        /// </summary>
        public void Start()
        {
            Start((int)SocketOptionName.MaxConnections);
        }

        /// <summary>
        /// Start Listening for Incoming TCP Connections
        /// </summary>
        /// <param name="backlog">The Maximum Number of Connections allowed in the Accept Queue</param>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public void Start(int backlog)
        {
            if(backlog < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(backlog), "The Backlog cannot be less than 0");
            }

            if(backlog > (int)SocketOptionName.MaxConnections)
            {
                throw new ArgumentOutOfRangeException(nameof(backlog), "The Backlog cannot be greater than " + (int)SocketOptionName.MaxConnections);
            }

            throwIfDisposed();

            if(_socket == null)
            {
                initializeSocket();
            }

            try
            {
                _socket?.Bind(_localEndPoint);

                _socket?.Listen(backlog);
            }
            catch (SocketException)
            {
                Stop();
                throw;
            }
        }

        /// <summary>
        /// Stop Listening for Incoming TCP Connections
        /// </summary>
        public void Stop()
        {
            if(_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }

        /// <summary>
        /// Accept an Incoming TCP Connection Asynchronously
        /// </summary>
        /// <returns>A <see cref="TcpClient"/> for the Accepted TCP Connection</returns>
        /// <exception cref="System.NullReferenceException"></exception>
        /// <exception cref="System.ObjectDisposedException"></exception>
        public async Task<TcpClient> AcceptAsync(CancellationToken cancellationToken)
        {
            throwIfDisposed();
            
            if(_socket == null)
            {
                throw new NullReferenceException("Attempting to Accept an Incoming TCP Connection while the TCP Listener is Stopped");
            }
            
            Socket clientSocket = await _socket.AcceptAsync(cancellationToken);

            return new TcpClient(clientSocket);
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
        /// Throws an Exception if this <see cref="TcpListener"/> instance has been Disposed
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
