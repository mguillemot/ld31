#region License
/*
NetIRC2
Copyright (c) 2013 James F. Bellinger <http://www.zer7.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using NetIrc2.Details;

namespace NetIrc2
{
    /// <summary>
    /// Answers requests using the Ident protocol (RFC 1413).
    /// Many IRC servers try to connect to a client's Ident server.
    /// </summary>
    [Category("Network")]
    [Description("Responds to Ident requests as expected by some Internet Relay Chat servers.")]
    public partial class IdentServer
    {
        HashSet<Connection> _connections = new HashSet<Connection>();
        IrcString _os, _user;
        TcpListener _listener;
        IAsyncResult _listenResult;

        public IdentServer()
        {

        }

        /// <summary>
        /// Starts the Ident server.
        /// </summary>
        /// <param name="port">The port to listen on. The standard port is 113.</param>
        public void Start(int port = 113)
        {
            Throw.If.Negative(port, "port");

            Start(new IPEndPoint(IPAddress.Any, port));
        }

        /// <summary>
        /// Starts the Ident server, listening on the specified endpoint.
        /// </summary>
        /// <param name="endPoint">The endpoint to listen on.</param>
        public void Start(IPEndPoint endPoint)
        {
            Throw.If.Null(endPoint, "endPoint");

            _listener = new TcpListener(endPoint);
            _listener.Start();
            Accept();
        }

        void Accept()
        {
            try
            {
                _listenResult = _listener.BeginAcceptTcpClient(HandleAcceptTcpClient, null);
            }
            catch (ObjectDisposedException)
            {
                // Close() was called.
                return;
            }
        }

        /// <summary>
        /// Stops the Ident server and disconnects all connected clients.
        /// </summary>
        public void Stop()
        {
            if (_listener != null) { _listener.Stop(); }

            if (_listenResult != null) { _listenResult.AsyncWaitHandle.WaitOne(); }

            lock (_connections)
            {
                foreach (var connection in _connections.ToArray())
                {
                    connection.Stop();
                }
            }
        }

        void HandleAcceptTcpClient(IAsyncResult result)
        {
            TcpClient client;
            try
            {
                client = _listener.EndAcceptTcpClient(result);
            }
            catch (ObjectDisposedException)
            {
                // Close() was called.
                return;
            }
            var stream = client.GetStream();

            var connection = new Connection() { Server = this, Stream = stream };
            connection.Start();

            Accept();
        }

        /// <summary>
        /// The name of the operating system running on the computer.
        /// 
        /// By default, WIN32 will be used on Windows, and UNIX will be used elsewhere.
        /// </summary>
        [AmbientValue(null)]
        public IrcString OperatingSystem
        {
            get { return _os ?? (Environment.OSVersion.Platform == PlatformID.Win32NT ? "WIN32" : "UNIX"); }
            set { _os = value; }
        }

        bool ShouldSerializeOperatingSystem()
        {
            return _os != null;
        }

        /// <summary>
        /// The Ident user ID to reply with.
        /// 
        /// Set this to match the IRC username.
        /// </summary>
        [AmbientValue(null)]
        public IrcString UserID
        {
            get { return _user ?? "netirc"; }
            set { _user = value; }
        }

        bool ShouldSerializeUserID()
        {
            return _user != null;
        }
    }
}
