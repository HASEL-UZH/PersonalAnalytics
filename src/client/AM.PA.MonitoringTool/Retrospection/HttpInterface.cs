// Created by André Meyer (ameyer@ifi.uzh.ch) from the University of Zurich
// Created: 2015-10-20
// 
// Licensed under the MIT License.
using Shared.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Web;

namespace Retrospection
{
    public delegate int HttpRequestHandlerDelegate(HttpReqResp reqResp);
    public class HttpServer
    {
        TcpListener _listener = null;
        bool _exitThread;
        readonly Dictionary<string, HttpRequestHandlerDelegate> _handlers = new Dictionary<string, HttpRequestHandlerDelegate>();
        readonly Dictionary<int, string> _statusCodes = new Dictionary<int, string>();
        int _clientCount = 0;
        public HttpServer()
        {
            _statusCodes[200] = "OK";
            _statusCodes[301] = "Moved Permanently";
            _statusCodes[400] = "Bad Request";
            _statusCodes[403] = "Forbidden";
            _statusCodes[404] = "Not Found";
        }
        public bool Start(int port, bool loopback)
        {
            _clientCount = 0;
            if (_listener != null)
                Stop();
            try
            {
                // only allow localhost address to access it (otherwise, privacy issues!)  
                //var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];  

                _listener = loopback
                    ? new TcpListener(IPAddress.Loopback, port)
                    : new TcpListener(IPAddress.Any, port);
                _listener.Start();
            }
            catch (Exception ex)
            {
                Database.GetInstance().LogError(ex.Message);
                return false;
            }
            _exitThread = false;
            new Thread(ListenerThread).Start();
            return true;
        }
        public void Stop()
        {
            _exitThread = true;

            if (_listener != null && _listener.Server != null)
            {
                if (_listener.Server.Connected)
                {
                    _listener.Server.Shutdown(SocketShutdown.Both);
                    _listener.Server.Disconnect(true);
                }
                _listener.Server.Close();
            }
            _listener.Stop();
            
            while (_listener != null)
                Thread.Sleep(100);
        }
        public void AddHandler(string token, HttpRequestHandlerDelegate handlerDelegate)
        {
            _handlers[token] = handlerDelegate;
        }
        private void ListenerThread()
        {
            while (!_exitThread)
            {
                var threadListener = new Thread(HandlerThread);
                threadListener.SetApartmentState(ApartmentState.STA);
                try { threadListener.Start(_listener.AcceptTcpClient()); }
                catch (Exception) { }
            }
            _listener = null;
        }
        private void HandlerThread(object obj)
        {
            if (_clientCount > 9)
                return;
            _clientCount++;
            var client = (TcpClient)obj;
            Stream stream = client.GetStream();
            try
            {
                var reader = new StreamReader(stream);
                var line = reader.ReadLine();
                if (line == null) line = string.Empty;
                else line = line.Split(' ')[1].Substring(1);
                var token = (line.IndexOf('?') == -1 ? line : line.Substring(0, line.IndexOf('?')));
                var query = (line.IndexOf('?') == -1 ? string.Empty : line.Substring(line.IndexOf('?') + 1));
                var host = "";
                var contentLength = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                        host = line.Substring(5).Trim().Split(':')[0];
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        contentLength = int.Parse(line.Substring(15).Trim(), CultureInfo.InvariantCulture);
                    else if (line == "")
                    {
                        if (contentLength != 0)
                        {
                            var buffer = new char[contentLength];
                            reader.Read(buffer, 0, contentLength);
                            query += (query == string.Empty ? "" : "&") + new string(buffer);
                        }
                        break;
                    }
                }

                var reqResp = new HttpReqResp(host, token, query);
                reqResp.SetHeader("Cache-Control", "no-cache");
                var statusCode = (_handlers.ContainsKey(token) ? _handlers[token](reqResp) : 404);
                WriteString("HTTP/1.1 " + statusCode + " " + _statusCodes[statusCode] + "\r\n", stream);
                WriteString("Server: aHTTP 1.1\r\n", stream);
                reqResp.SetHeader("Content-Length", reqResp.Response.Length.ToString(CultureInfo.InvariantCulture));
                for (var i = 0; i < reqResp.Headers.Count; i++)
                    WriteString(reqResp.Headers.GetKey(i) + ": " + reqResp.Headers.Get(i) + "\r\n", stream);
                WriteString("\r\n", stream);
                try { stream.Write(reqResp.Response.GetBuffer(), 0, (int)reqResp.Response.Length); }
                catch { }
            }
            catch { }
            stream.Flush();
            stream.Close();
            client.Close();
            _clientCount--;
        }
        private void WriteString(string str, Stream stream)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            try { stream.Write(bytes, 0, bytes.Length); }
            catch (Exception) { }
        }
    }
    public class HttpReqResp
    {
        readonly string _host;
        readonly string _script;
        readonly NameValueCollection _parameters;
        readonly NameValueCollection _headers;
        readonly MemoryStream _response;

        internal HttpReqResp(string host, string script, string query)
        {
            _host = host;
            _script = script;
            _parameters = HttpUtility.ParseQueryString(query);
            _headers = new NameValueCollection();
            _response = new MemoryStream();
        }
        public string Host
        {
            get { return _host; }
        }
        public string Script
        {
            get { return _script; }
        }
        public string this[string name]
        {
            get { return _parameters[name]; }
        }
        internal NameValueCollection Headers
        {
            get { return _headers; }
        }
        internal MemoryStream Response
        {
            get { return _response; }
        }
        public void SetHeader(string name, string value)
        {
            _headers.Add(name, value);
        }
        public void Write(byte[] bytes)
        {
            _response.Write(bytes, 0, bytes.Length);
        }
        public void Write(string str)
        {
            Write(Encoding.UTF8.GetBytes(str));
        }
        public void WriteLine(string str)
        {
            Write(str + "\r\n");
        }
    }
}