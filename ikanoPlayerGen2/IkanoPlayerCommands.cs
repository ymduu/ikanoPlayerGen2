using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using CoreTweet;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace ikanoPlayerGen2
{
    [Group("ikanoplayer")]
    public class IkanoPlayerCommands : ModuleBase
    {
        private static Tokens IkanoPlayerTokens;
        private static IkanoPlayerDb Db = new IkanoPlayerDb();

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

        public async Task<Discord.IGuildUser> GetUserFromNickname(Discord.IGuild guild, string nickname)
        {
            //NicknameのユーザーID取得、nicknameがユニークである想定、そうでなかったら動作未定義
            var list = await guild.GetUsersAsync();
            try
            {
                var user = list.Where(addUser => addUser.Nickname == nickname || addUser.Username == nickname).First();
                return user;
            }
            catch
            {
                throw;
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

        [Command("add")]
        public async Task<RuntimeResult> Add(string twitterId)
        {
            //AuthorのユーザーID取得
            Discord.IGuildUser user = (Discord.IGuildUser)Context.Message.Author;
            string dbName = user.Nickname == null ? user.Username : user.Nickname;
            RuntimeResult result = Db.AddUser(twitterId, dbName, user.Id.ToString(), Context.Guild.Id.ToString());
            if(!result.IsSuccess)
            {
                return result;
            }
            Console.WriteLine(user.Nickname + " Added.");
            await ReplyAsync(String.Format("user added. Twitter: {0} Nickname: {1} Id: {2}", twitterId, user.Nickname, user.Id.ToString()));
            return IkanoPlayerResult.FromSuccess();
        }

        [Command("add")]
        public async Task<RuntimeResult> Add(string twitterId, string nickname)
        {
            Discord.IGuildUser user = null;
            try
            {
                user = await GetUserFromNickname(Context.Guild, nickname);
            }
            catch(InvalidOperationException e)
            {
                Console.WriteLine(e);
                return IkanoPlayerResult.FromError("そんなnicknameの人知りません。");
            }

            string dbName = user.Nickname == null ? user.Username : user.Nickname;

            RuntimeResult result = Db.AddUser(twitterId, dbName, user.Id.ToString(), Context.Guild.Id.ToString());
            if (!result.IsSuccess)
            {
                return result;
            }
            Console.WriteLine(dbName + " Added.");
            await ReplyAsync(String.Format("user added. Twitter: {0} Nickname: {1} Id: {2}", twitterId, dbName, user.Id.ToString()));
            return IkanoPlayerResult.FromSuccess();
        }
        [Command("remove")]
        public async Task<RuntimeResult> Remove()
        {
            //AuthorのユーザーID取得
            Discord.IGuildUser user = (Discord.IGuildUser)Context.Message.Author;
            string dbName = user.Nickname == null ? user.Username : user.Nickname;
            RuntimeResult result = Db.RemoveUser(dbName, user.Id.ToString(), Context.Guild.Id.ToString());
            if (!result.IsSuccess)
            {
                return result;
            }
            Console.WriteLine(user.Nickname + " Removed.");
            await ReplyAsync(String.Format("user Removed. Nickname: {0} Id: {1}", user.Nickname, user.Id.ToString()));
            return IkanoPlayerResult.FromSuccess();
        }

        [Command("remove")]
        public async Task<RuntimeResult> Remove(string nickname)
        {

            Discord.IGuildUser user = null;
            try
            {
                user = await GetUserFromNickname(Context.Guild, nickname);
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
                return IkanoPlayerResult.FromError("そんなnicknameの人知りません。");
            }

            string dbName = user.Nickname == null ? user.Username : user.Nickname;

            RuntimeResult result = Db.RemoveUser(dbName, user.Id.ToString(), Context.Guild.Id.ToString());
            if (!result.IsSuccess)
            {
                return result;
            }
            Console.WriteLine(user.Nickname + " Removed.");
            await ReplyAsync(String.Format("user Removed. Nickname: {0} Id: {1}", user.Nickname, user.Id.ToString()));
            return IkanoPlayerResult.FromSuccess();
        }

        [Command("show")]
        public async Task<RuntimeResult> Show()
        {
            List<IkanoPlayerUser> registeredList;
            registeredList = Db.GetAllUserInServer(Context.Guild.Id.ToString());
            string postStr = "";

            if(registeredList.Count == 0)
            {
                return IkanoPlayerResult.FromError("このサーバーに登録されている人がいません。");
            }

            foreach(IkanoPlayerUser user in registeredList)
            {
                postStr += user.ToString();
            }

            await ReplyAsync(postStr);
            return IkanoPlayerResult.FromSuccess();
        }
    }
}
