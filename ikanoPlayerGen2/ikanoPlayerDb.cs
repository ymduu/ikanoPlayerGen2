using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace ikanoPlayerGen2
{
    public class IkanoPlayerDb :IDisposable
    {
        private SQLiteConnectionStringBuilder SqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = "ikanoPlayer.db" };
        private SQLiteConnection Connection;

        public IkanoPlayerDb()
        {
            //DB接続、ない場合テーブル作成
            Connection = new SQLiteConnection(SqlConnectionSb.ToString());
            if(Connection == null)
            {
                Console.WriteLine("DB接続に失敗しました。");
            }
            Connection.Open();

            //ない場合自動でテーブル作成
            using (var cmd = new SQLiteCommand(Connection))
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ikanoplayer_user(" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "twitter_id TEXT NOT NULL," +
                    "discord_display_name TEXT NOT NULL," +
                    "discord_id TEXT NOT NULL," +
                    "discord_server_id TEXT NOT NULL)";
                    
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ikanoplayer_tweet(" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "post_id TEXT NOT NULL," +
                    "discord_server_id TEXT NOT NULL," +
                    "discord_channel_id TEXT NOT NULL)";

                cmd.ExecuteNonQuery();
            }

        }

        public RuntimeResult AddUser(string twitterId, string discordDisplayName, string discordId, string discordServerId)
        {
            using (var cmd = new SQLiteCommand(Connection))
            {
                // 
                cmd.CommandText = "SELECT COUNT(*) from ikanoplayer_user WHERE discord_id = :discordId and discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;
                long exists = (long)cmd.ExecuteScalar();

                if(exists >= 1)
                {
                    return IkanoPlayerResult.FromError(discordDisplayName + " さんはこのサーバーのinviteリストにすでに追加されています。");
                }

                cmd.CommandText = "INSERT INTO ikanoplayer_user(twitter_id, discord_display_name, discord_id, discord_server_id) VALUES(:twitterId, :discordDisplayName, :discordId, :discordServerId)";
                cmd.Parameters.Add("twitterId", System.Data.DbType.String).Value = twitterId;
                cmd.Parameters.Add("discordDisplayName", System.Data.DbType.String).Value = discordDisplayName;
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;

                cmd.ExecuteNonQuery();
                return IkanoPlayerResult.FromSuccess();
            }
        }

        public RuntimeResult RemoveUser(string discordDisplayName, string discordId, string discordServerId)
        {
            using (var cmd = new SQLiteCommand(Connection))
            {
                // 
                cmd.CommandText = "SELECT COUNT(*) from ikanoplayer_user WHERE discord_id = :discordId and discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;
                long exists = (long)cmd.ExecuteScalar();

                if (exists == 0)
                {
                    return IkanoPlayerResult.FromError(discordDisplayName + " さんはこのサーバーのinviteリストに登録されていません。");
                }

                cmd.CommandText = "DELETE FROM ikanoplayer_user WHERE discord_id = :discordId and discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;

                cmd.ExecuteNonQuery();
                return IkanoPlayerResult.FromSuccess();
            }
        }

        internal List<IkanoPlayerUser> GetAllUserInServer(string discordServerId)
        {
            List<IkanoPlayerUser> retList = new List<IkanoPlayerUser>();

            using (var cmd = new SQLiteCommand(Connection))
            {
                
                cmd.CommandText = "SELECT * from ikanoplayer_user WHERE discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        IkanoPlayerUser user = new IkanoPlayerUser(reader["twitter_id"].ToString(), reader["discord_display_name"].ToString(), reader["discord_id"].ToString(), reader["discord_server_id"].ToString());
                        retList.Add(user);
                    }
                }

            }
            return retList;
        }

        internal RuntimeResult AddInvite(List<IkanoPlayerTweet> inviteList)
        {
            //既存のinviteを削除
            using (var cmd = new SQLiteCommand(Connection))
            {
                //既にそのチャンネルでinviteが行われているかをチェック 
                cmd.CommandText = "SELECT COUNT(*) from ikanoplayer_tweet WHERE discord_channel_id = :discordChannelId and discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordChannelId", System.Data.DbType.String).Value = inviteList[0].DiscordChannelId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = inviteList[0].DiscordServerId;
                long exists = (long)cmd.ExecuteScalar();

                if (exists >= 1)
                {
                    //既にされているinviteを削除
                    cmd.CommandText = "DELETE FROM ikanoplayer_tweet WHERE discord_channel_id = :discordChannelId and discord_server_id = :discordServerId";
                    cmd.Parameters.Add("discordChannelId", System.Data.DbType.String).Value = inviteList[0].DiscordChannelId;
                    cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = inviteList[0].DiscordServerId;

                    cmd.ExecuteNonQuery();
                }
            }

            foreach (IkanoPlayerTweet tweet in inviteList)
            {

                using (var cmd = new SQLiteCommand(Connection))
                {
                    cmd.CommandText = "INSERT INTO ikanoplayer_tweet(post_id, discord_channel_id, discord_server_id) VALUES(:postId, :discordChannelId, :discordServerId)";
                    cmd.Parameters.Add("postId", System.Data.DbType.String).Value = tweet.PostId;
                    cmd.Parameters.Add("discordChannelId", System.Data.DbType.String).Value = tweet.DiscordChannelId;
                    cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = tweet.DiscordServerId;

                    cmd.ExecuteNonQuery();
                }
            }
            return IkanoPlayerResult.FromSuccess();
        }
    

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
