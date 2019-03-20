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

        public static async void GetReplyThreadBody()
        {


            while (true)
            {
                try
                {
                    Init();

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
                        //UserStreamっぽく眺めてニコニコする用
                        /*
                        if (idList.Contains((long)m.User.Id))
                        {
                            Console.WriteLine("{0}: {1}", m.User.ScreenName, m.Text);
                        }
                        */

                        if (m.InReplyToStatusId.HasValue)
                        {
                            string channelIdStr = Db.GetChannelFromTweet(m.InReplyToStatusId.ToString());

                            if (channelIdStr == null)
                            {
                                continue;
                            }

                            ulong channelId = new ulong();
                            if (ulong.TryParse(channelIdStr, out channelId))
                            {
                                var channel = ikanoPlayerGen2.Program.client.GetChannel(channelId) as Discord.IMessageChannel;
                                string sendUrl = string.Format("https://twitter.com/{0}/status/{1}", m.User.ScreenName, m.Id.ToString());
                                await channel.SendMessageAsync("ikanoplayer got reply: \n" + sendUrl);
                            }
                        }

                    }
                }
                catch(System.IO.IOException e)
                {
                    Console.WriteLine("Twitter Disconnected. try after 1 min.");
                    await Task.Delay(60000);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Unknown Exception");
                    Console.WriteLine(e.ToString());
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

        [Command("help"), Summary("助けはこないよ")]
        public async Task Help()
        {
            var sb = new StringBuilder();
            sb.AppendLine("*ikanoplayer2*");
            sb.AppendLine();
            sb.AppendLine("ikanoplayer2からはコマンドの先頭に`!`をつけます。これさえ覚えておけば旧ikanoplayerと大差ないです。");


            var builder = new Discord.EmbedBuilder();
            foreach (var c in Program.commands.Commands)
            {
                builder.AddField(
                    c.Aliases.First() + " " + string.Join(" ", c.Parameters.Select(x => $"[{x}]")),
                    (c.Summary ?? "no description") + "\n" +
                        string.Join("\n", c.Parameters.Select(x => $"[{x.Name}]: {x.Summary}"))
                );
            }
            await ReplyAsync(sb.ToString(), false, builder.Build());
        }

        /// <summary>
        /// テスト用、パラメータをechoするだけ
        /// </summary>
        /// <returns>空白区切りのパラメータ</returns>
        [Command("echoParams"), Summary("複数パラメータに即対応できてすごい(すごい)")]
        public async Task EchoParams(string param1, string param2, string param3)
        {
            await Context.Channel.SendMessageAsync($"these are params{param1}, {param2}, {param3}");
        }

        [Command("add"), Summary("投稿者を追加します。")]
        public async Task<RuntimeResult> Add([Summary("ツイッタ～のID")] string twitterId)
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

        [Command("add"), Summary("自分以外を追加したいとき用です。表示名(nickname)とか言いつつユーザ名でも解決します。")]
        public async Task<RuntimeResult> Add([Summary("ツイッタ～のID")] string twitterId, [Summary("表示名")] string nickname)
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

        [Command("addById"), Summary("移行するための機能なので使わなくていいです。")]
        public async Task<RuntimeResult> AddById([Summary("ツイッタ～のID")] string twitterId, [Summary("ディスコ～ドのID")] ulong discordId)
        {
            Discord.IGuildUser user = await Context.Guild.GetUserAsync(discordId) as Discord.IGuildUser;

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

        [Command("remove"), Summary("投稿者をデータベースから消します。")]
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

        [Command("remove"), Summary("自分以外を消したいときにはremoveに表示名を指定することもできます。add同様ユーザ名でも解決します。")]
        public async Task<RuntimeResult> Remove([Summary("表示名")] string nickname)
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

        [Command("show"), Summary("登録されている人の一覧を表示します。")]
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
        [Command("invite"), Summary("twitter にinviteを送ります。")]
        public async Task<RuntimeResult> Invite([Summary("リプライとして送る文字列")] string inviteStr)
        {
            List<IkanoPlayerUser> registeredList;
            registeredList = Db.GetAllUserInServer(Context.Guild.Id.ToString());
            string postStr = "";

            if (registeredList.Count == 0)
            {
                return IkanoPlayerResult.FromError("このサーバーに登録されている人がいません。");
            }

            List<IkanoPlayerTweet> ikanoPlayerInvites = new List<IkanoPlayerTweet>();

            foreach (IkanoPlayerUser user in registeredList)
            {
                postStr += ("@" + user.TwitterId + " ");
                if(postStr.Length >= 100)
                {
                    StatusResponse response = await IkanoPlayerTokens.Statuses.UpdateAsync(postStr + inviteStr);
                    postStr = "";
                    IkanoPlayerTweet tweet = new IkanoPlayerTweet(response.Id.ToString(), Context.Guild.Id.ToString(), Context.Channel.Id.ToString());
                    ikanoPlayerInvites.Add(tweet);
                }
            }

            if(postStr != "")
            {
                StatusResponse response = await IkanoPlayerTokens.Statuses.UpdateAsync(postStr + inviteStr);
                postStr = "";
                IkanoPlayerTweet tweet = new IkanoPlayerTweet(response.Id.ToString(), Context.Guild.Id.ToString(), Context.Channel.Id.ToString());
                ikanoPlayerInvites.Add(tweet);
            }
            Db.AddInvite(ikanoPlayerInvites);
            await ReplyAsync("inviteが送られました");
            return IkanoPlayerResult.FromSuccess();
        }

        [Command("invite"), Summary("twitter にinviteを送ります。パラメータがない場合はリプライのみ送信します。")]
        public async Task<RuntimeResult> Invite()
        {
            return await Invite("");
        }
    }
}
