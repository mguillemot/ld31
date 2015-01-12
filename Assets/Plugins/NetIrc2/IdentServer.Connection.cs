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
using System.IO;
using System.Net.Sockets;

namespace NetIrc2
{
    public partial class IdentServer
    {
        sealed class Connection
        {
            public IdentServer Server { get; set; }
            public NetworkStream Stream { get; set; }

            byte[] _inBuffer = new byte[IrcConstants.MaxIdentLineLength];
            int _inOffset;
            IAsyncResult _readResult;
            IAsyncResult _writeResult;

            public void Start()
            {
                lock (Server._connections)
                {
                    Server._connections.Add(this);
                }

                try
                {
                    _readResult = Stream.BeginRead(_inBuffer, _inOffset, _inBuffer.Length - _inOffset, HandleRead, null);
                }
                catch (IOException)
                {
                    Stop(); return;
                }
                catch (ObjectDisposedException)
                {
                    Stop(); return;
                }
            }

            public void Stop()
            {
                Stream.Close();
                var rd = _readResult; if (rd != null) { rd.AsyncWaitHandle.WaitOne(); }
                var wr = _writeResult; if (wr != null) { wr.AsyncWaitHandle.WaitOne(); }

                lock (Server._connections)
                {
                    Server._connections.Remove(this);
                }
            }

            void HandleRead(IAsyncResult result)
            {
                int count = 0;
                try { count = Stream.EndRead(result); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
                if (count == 0) { Stop(); return; }
                _inOffset += count;

                while (true)
                {
                    int crlfIndex = IrcString.IndexOf(_inBuffer, @byte => @byte == 13 || @byte == 10, 0, _inOffset);
                    if (crlfIndex == -1)
                    {
                        if (_inOffset == _inBuffer.Length) { Stop(); return; }
                        Start(); return;
                    }

                    var query = new IrcString(_inBuffer, 0, crlfIndex);
                    Array.Copy(_inBuffer, crlfIndex + 1, _inBuffer, 0, _inOffset - (crlfIndex + 1));
                    _inOffset -= crlfIndex + 1;

                    if (query.Length > 0 && !query.Contains(0)) // NULL is not allowed in Ident, so we'll just ignore it.
                    {
                        var response = Server.GetQueryResponse(query);
                        if (response != null && response.Length > 0)
                        {
                            var responseBytes = (response + "\r\n").ToByteArray();

                            try
                            {
                                _writeResult = Stream.BeginWrite(responseBytes, 0, responseBytes.Length, HandleWrite, null);
                            }
                            catch (IOException)
                            {
                                Stop(); return;
                            }
                            catch (ObjectDisposedException)
                            {
                                Stop(); return;
                            }

                            return;
                        }
                    }
                }
            }

            void HandleWrite(IAsyncResult result)
            {
                try
                {
                    Stream.EndWrite(result);
                }
                catch (IOException)
                {
                    Stop(); return;
                }
                catch (ObjectDisposedException)
                {
                    Stop(); return;
                }

                Start();
            }
        }
    }
}
