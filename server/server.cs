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
using System.Web;

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
        string _filename = "chat.json";
        private Out _chat;
        private List<Message> _messages = new List<Message>();
        private Dictionary<string, DateTime> _online = new Dictionary<string, DateTime>();

        public int Port {
            get { return _port; }
            private set { }
        }

        public void Refresh() {
            List<string> online = new List<string>();
            try {
                foreach (KeyValuePair<string, DateTime> pair in _online.ToList()) {
                    DateTime time = pair.Value;
                    string name = pair.Key;

                    if (time < DateTime.Now) {
                        _online.Remove(name);
                    } else {
                        online.Add(name);
                    }

                }

                if (_messages.Count > 100) {
                    while(_messages.Count != 100) {
                        _messages.RemoveAt(_messages.Count - 1);
                        Console.WriteLine("Message List Over 100, Removing Value At {0}", _messages.Count);
                    }
                }
                _chat = new Out(online, _messages);

                File.WriteAllText(_filename, JsonConvert.SerializeObject(_chat));
            } catch (Exception e) {
                Console.WriteLine("Error In Refreshing Online Users");
                Console.WriteLine(e.Message);
            }
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
                } catch (Exception ex) {

                }
            }
        }

        private void Process(HttpListenerContext context) {
            string path = context.Request.Url.AbsolutePath;
            path = HttpUtility.UrlDecode(path);
            path = path.Substring(1);

            string[] names = path.Split('/');
            foreach (string s in names)
                Console.Write(s + ", ");
            Console.WriteLine();

            if (names[0].Equals("in")) {
                try {
                    string message = names[1];

                    Message messagep = JsonConvert.DeserializeObject<Message>(message);

                    _messages.Add(messagep);

                    Refresh();

                    context.Response.SendJSON(File.ReadAllText(_filename));

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();
                } catch (Exception ex) {
                    context.Response.SendMessage(ex.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    context.Response.OutputStream.Flush();
                }
            } else if (names[0].Equals("out")) {
                try {
                    string username = names[1];

                    _online.Add(username, DateTime.Now.AddMinutes(1));

                    //get online users
                    Refresh();

                    Stream input = new FileStream(_filename, FileMode.Open);

                    //Adding permanent http response headers
                    context.Response.ContentLength64 = input.Length;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(_filename).ToString("r"));
                    input.Close();

                    context.Response.SendJSON(File.ReadAllText(_filename));

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.OutputStream.Flush();

                } catch (Exception ex) {
                    context.Response.SendMessage(ex.Message);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    context.Response.OutputStream.Flush();
                }
            } else if (names[0].Equals("ping")) {
                context.Response.SendMessage("Pong");
            } else {
                context.Response.SendMessage("Please Use /in To Send Messages Or /out To Get The Last 100 Messages");

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

            string chat = File.ReadAllText(_filename);
            Out _chat = JsonConvert.DeserializeObject<Out>(chat);
            _messages = _chat.messages;
        }


    }

    static class Extentions {
        public static void SendJSON(this HttpListenerResponse outs, String tosend) {
            outs.AddHeader("Access-Control-Allow-Origin", "*");
            outs.ContentType = "application/json";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tosend);
            outs.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public static void SendMessage(this HttpListenerResponse outs, String tosend) {
            outs.AddHeader("Access-Control-Allow-Origin", "*");
            tosend = JsonConvert.SerializeObject(new Alert(tosend));
            outs.ContentType = "application/json";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tosend);
            outs.OutputStream.Write(bytes, 0, bytes.Length);
        }

        [Obsolete("Use SendMessage() Instead", true)]
        public static void SendText(this HttpListenerResponse outs, String tosend) {
            outs.AddHeader("Access-Control-Allow-Origin", "*");
            outs.ContentType = "application/json";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(tosend);
            outs.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }

    public class Out {
        public Out(List<String> online, List<Message> messages) {
            this.online = online;
            this.messages = messages;
        }
        public List<String> online { get; }
        public List<Message> messages { get; }
    }

    public class Message {
        public Message(string name, string color, string text, List<String> mentioned) {
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

    public class Alert {
        public Alert(string message) {
            this.message = message;
        }
        public string message { get; }
    }

}
