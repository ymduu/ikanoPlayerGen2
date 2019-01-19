using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ikanoPlayerGen2
{
    class IkanoPlayerUser
    {
        string TwitterId;
        string DiscordDisplayName;
        string DiscordId;
        string DiscordServerId;

        public IkanoPlayerUser(string twitterId, string discordDisplayName, string discordId, string discordServerId)
        {
            TwitterId = twitterId;
            DiscordDisplayName = discordDisplayName;
            DiscordId = discordId;
            DiscordServerId = discordServerId;
        }
        public override string ToString() 
        {
            return $"[@{TwitterId} {DiscordDisplayName} {DiscordId}]\n";
        }
    }
}
