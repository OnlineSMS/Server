using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Security.Principal;

namespace SMOL {
    class Program {

        static void Main(string[] args) {
            if (IsAdministrator()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                //VARS
                string config = @"config.json";
                string chat = @"chat.json";
                string title = @"Online SMS";
                string json = JsonConvert.SerializeObject(new Config(4269, title));
                string chatj = JsonConvert.SerializeObject(new Out(new List<string>(), new List<Message>()));
                //Makes Config
                if (!File.Exists(config)) {
                    File.Create(config).Close();
                    File.WriteAllText(config, json);
                    sysout(string.Format("Creating {0}", config));
                }
                //Makes Chat
                if (!File.Exists(chat)) {
                    File.Create(chat).Close();
                    File.WriteAllText(chat, chatj);
                    sysout(string.Format("Creating {0}", chat));
                }
                //Read Config
                string jek = File.ReadAllText(config);
                Config jekk = JsonConvert.DeserializeObject<Config>(jek);
                //Make Server
                Console.Title = jekk.title;
                SimpleSMSServer server = new SimpleSMSServer("\\", jekk.port);
                //Hi
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Online SMS Webserver Started On Port {0}", jekk.port);
                Console.WriteLine("Type Exit Or Stop To Close The Server");
                Console.ForegroundColor = ConsoleColor.Gray;
                //exit
                while (true) {
                    string inp = Console.ReadLine().ToLower();
                    if (inp.Equals("exit") || inp.Equals("stop")) {
                        break;
                    }
                }
                server.Stop();
            } else {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You Do Not Have Administrator Permisions");
            }
            Console.ReadKey();
        }

        public static bool IsAdministrator() {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void sysout(string msg, bool OmitDate) {
            if (!OmitDate)
                msg = string.Format("[{0:d} {0:t}] {1}", DateTime.Now, msg);
            Console.WriteLine(msg);
        }

        public static void sysout(string msg) { sysout(msg, false); }

    }

    class Config {
        public Config(int port, string title) {
            this.port = port;
            this.title = title;
        }
        public int port { get; }
        public string title { get; }
    }

}