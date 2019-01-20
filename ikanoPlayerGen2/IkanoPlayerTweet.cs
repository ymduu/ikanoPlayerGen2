using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ikanoPlayerGen2
{
    class IkanoPlayerTweet
    {
        public IkanoPlayerTweet(string postId, string discordServerId, string discordChannelId)
        {
            PostId = postId;
            DiscordServerId = discordServerId;
            DiscordChannelId = discordChannelId;
        }

        public string PostId;
        public string DiscordServerId;
        public string DiscordChannelId;
    }
}
