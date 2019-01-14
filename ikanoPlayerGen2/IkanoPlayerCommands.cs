using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Generic;
using CoreTweet;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ikanoPlayerGen2
{
    public class IkanoPlayerCommands : ModuleBase
    {
        private static Tokens IkanoPlayerTokens;

        //認証情報取得
        public static void GetAuthInfo()
        {
            OAuth.OAuthSession Session;
            Session = OAuth.Authorize(Constants.TWITTER_CK, Constants.TWITTER_CS);
            System.Diagnostics.Process.Start(Session.AuthorizeUri.AbsoluteUri);

            Console.Write("Please Input PIN >>");
            string pin = Console.ReadLine();

            IkanoPlayerTokens = Session.GetTokens(pin);

            Console.WriteLine("IkanoPlayerGen2 Login OK");
            Console.WriteLine("AT: " + IkanoPlayerTokens.AccessToken);
            Console.WriteLine("AS: " + IkanoPlayerTokens.AccessTokenSecret);
        }

        public static void Init()
        {
            IkanoPlayerTokens = Tokens.Create(Constants.TWITTER_CK, Constants.TWITTER_CS, Constants.TWITTER_ACCESS_TOKEN, Constants.TWITTER_ACCESS_TOKEN_SECRET);

            //IkanoPlayerTokens.Statuses.Update(new { status = "IkanoPlayerGen2 Up: " + DateTime.Now.ToString() });

            Console.WriteLine("IkanoPlayerGen2 Login OK");
        }

        public static void GetReplyThreadBody()
        {
            //フォローしている人だけ取得
            var ids = IkanoPlayerTokens.Friends.Ids(screen_name: Constants.TWITTER_BOT_SCREEN_NAME);
            List<Int64> idList = ids.ToList<Int64>();

            foreach (var m in IkanoPlayerTokens.Streaming.Filter(follow: idList).OfType<CoreTweet.Streaming.StatusMessage>().Select(x => x.Status))
            {
                if (m.User.Id == null) 
                {
                    Console.WriteLine("null"); 
                    continue;
                }

                if (idList.Contains((long)m.User.Id))
                {
                    Console.WriteLine("{0}: {1}", m.User.ScreenName, m.Text);
                }
                
            }

        }

        /// <summary>
        /// テスト用、パラメータをechoするだけ
        /// </summary>
        /// <returns>空白区切りのパラメータ</returns>
        [Command("echoParams")]
        public async Task EchoParams(string param1, string param2, string param3)
        {
            await Context.Channel.SendMessageAsync($"these are params{param1}, {param2}, {param3}");
        }
    }
}
