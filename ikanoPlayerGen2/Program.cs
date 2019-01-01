using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ikanoPlayerGen2
{
    class Program
    {
        static void Main(string[] args)
        {
            //IkanoPlayerCommands.GetAuthInfo();
            IkanoPlayerCommands.Init();
            IkanoPlayerCommands.GetReplyThreadBody();
        }
    }
}
