using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace ikanoPlayerGen2
{
    class IkanoPlayerResult : RuntimeResult
    {
        //失敗ケース
        IkanoPlayerResult(CommandError? error, string reason) : base(error, reason)
        {
        }

        public static IkanoPlayerResult FromSuccess()
        {
            return new IkanoPlayerResult(null, "Success!");
        }

        public static IkanoPlayerResult FromError(string reason)
        {
            return new IkanoPlayerResult(CommandError.Unsuccessful, reason);
        }
    }
}
