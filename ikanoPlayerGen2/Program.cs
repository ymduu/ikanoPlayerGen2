﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ikanoPlayerGen2
{
    class Program
    {
        static void Main(string[] args)
        {
            IkanoPlayerCommands.Init();

            IkanoPlayerCommands.GetReplyThreadBody();

            var twitterTask = Task.Run(() =>
            {
                IkanoPlayerCommands.GetReplyThreadBody();
            });
            
        }
    }
}
