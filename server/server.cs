using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SMOL {
    class SimpleSMSServer {
        private readonly string[] _indexFiles = {
        "index.html",
        "index.htm",
        "default.html",
        "default.htm"
    };
        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private int _port;
        private Out _chat;
        private List<String> _online;
        private List<Message> _messages;
        string _filename = "chat.json";

        public int Port {
            get { return _port; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleSMSServer(string path, int port) {
            this.Initialize(path, port);
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleSMSServer(string path) {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Initialize(path, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop() {
            _serverThread.Abort();
            _listener.Stop();
        }

        private void Listen() {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://+:" + _port.ToString() + "/");
            try {
                _listener.Start();
            } catch (HttpListenerException e) {
                Program.sysout(string.Format(@"Failed to listen on port '{0}' because it conflicts with an existing registration on the machine.", _port.ToString()));
            }
            while (true) {
                try {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                    string chat = File.ReadAllText(_filename);
                    Out _chat = JsonConvert.DeserializeObject<Out>(chat);
                    _online = _chat.online;
                    _messages = _chat.messages;
                } catch (Exception ex) {

                }
            }
        }

        private void Process(HttpListenerContext context) {
            string path = context.Request.Url.AbsolutePath;
            path = path.Substring(1);
            Console.WriteLine(path);

            string[] names = path.Split('/');
            foreach (string s in names)
                Console.Write(s + ",");
            Console.WriteLine();

            if (names[0].Equals("in")) {

            } else if (names[0].Equals("out")) {
                try {
                    Stream input = new FileStream(_filename, FileMode.Open);

                    //Adding permanent http response headers
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(_filename).ToString("r"));
                    input.Close();

                    context.Response.OutputStream.SendString(File.ReadAllText(_filename));

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                } catch (Exception ex) {
                    context.Response.OutputStream.SendString(ex.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    context.Response.OutputStream.Flush();
                }
            } else {
                context.Response.OutputStream.SendString("Please Use /in To Send Messages Or /out To Get The Last 100 Messages");

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.OutputStream.Flush();
            }

            context.Response.OutputStream.Close();
        }

        private void Initialize(string path, int port) {
            this._rootDirectory = path;
            this._port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.Start();
        }


    }

    static class Extentions {
        public static void SendString(this Stream outs, String tosend) {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tosend);
            outs.Write(bytes, 0, bytes.Length);
        }
    }

    class Out {
        public Out(List<String> online, List<Message> messages) {
            this.online = online;
            this.messages = messages;
        }
        public List<String> online { get; }
        public List<Message> messages { get; }
    }

    class Message {
        Message(string name, string color, string text, List<String> mentioned) {
            this.name = name;
            this.color = color;
            this.text = text;
            this.mentioned = mentioned;
        }
        public string name { get; }
        public string color { get; }
        public string text { get; }
        public List<String> mentioned { get; }
    }

    class In {
        In(Message message) {
            this.message = message;
        }
        public Message message { get; }
    }
}
