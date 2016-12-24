using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Security.Principal;
using System.Windows.Shell;

namespace SMOL {
    class Program {

        static void Main(string[] args) {
            if (IsAdministrator()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                //VARS
                string config = @"SMOL_Config.json";
                string www = @"www\";
                string title = @"SMOL";
                string json = JsonConvert.SerializeObject(new Config(80, title));
                //Makes Config
                if (!File.Exists(config)) {
                    File.Create(config).Close();
                    File.WriteAllText(config, json);
                    sysout(string.Format("Creating {0}", config));
                }
                if (!Directory.Exists(www)) {
                    Directory.CreateDirectory(www);
                    sysout(string.Format("Creating {0}", www));
                }
                //Read Config
                string jek = File.ReadAllText(config);
                Config jekk = JsonConvert.DeserializeObject<Config>(jek);
                //Make Server
                Console.Title = jekk.title;
                SimpleHTTPServer server = new SimpleHTTPServer(www, jekk.port);
                //Hi
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SMOL Webserver Started On Port {0}", jekk.port);
                Console.WriteLine("Type Exit Or Stop To Close The Webserver");
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