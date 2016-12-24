using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server {
    class Program {
        static void Main(string[] args) {

        }

        public static void sysout(string msg, bool OmitDate) {
            if (!OmitDate)
                msg = string.Format("[{0:d} {0:t}] {1}", DateTime.Now, msg);
            Console.WriteLine(msg);
        }

        public static void sysout(string msg) { sysout(msg, false); }

    }
}
