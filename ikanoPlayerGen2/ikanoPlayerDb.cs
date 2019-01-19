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
        //Dbアクセスに関するresultクラス
        class IkanoPlayerDbResult : IResult
        {
            //失敗ケース
            IkanoPlayerDbResult(CommandError commandError, string errorReason, bool isSuccess)
            {
                Error = commandError;
                ErrorReason = errorReason;
                IsSuccess = isSuccess;
            }
            //成功ケース
            IkanoPlayerDbResult()
            {
                IsSuccess = true;
            }
            public CommandError? Error { get; }

            public string ErrorReason { get; }

            public bool IsSuccess { get; }

            public static IkanoPlayerDbResult FromSuccess()
            {
                return new IkanoPlayerDbResult();
            }

            public static IkanoPlayerDbResult FromError(string reason)
            {
                return new IkanoPlayerDbResult(CommandError.Unsuccessful, reason, false);
            }
        }


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
            }

        }

        public IResult AddUser(string twitterId, string discordDisplayName, string discordId, string discordServerId)
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
                    return IkanoPlayerDbResult.FromError(discordDisplayName + " さんはこのサーバーのinviteリストにすでに追加されています。");
                }

                cmd.CommandText = "INSERT INTO ikanoplayer_user(twitter_id, discord_display_name, discord_id, discord_server_id) VALUES(:twitterId, :discordDisplayName, :discordId, :discordServerId)";
                cmd.Parameters.Add("twitterId", System.Data.DbType.String).Value = twitterId;
                cmd.Parameters.Add("discordDisplayName", System.Data.DbType.String).Value = discordDisplayName;
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;

                cmd.ExecuteNonQuery();
                return IkanoPlayerDbResult.FromSuccess();
            }
        }

        public IResult RemoveUser(string discordDisplayName, string discordId, string discordServerId)
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
                    return IkanoPlayerDbResult.FromError(discordDisplayName + " さんはこのサーバーのinviteリストに登録されていません。");
                }

                cmd.CommandText = "DELETE FROM ikanoplayer_user WHERE discord_id = :discordId and discord_server_id = :discordServerId";
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;
                cmd.Parameters.Add("discordServerId", System.Data.DbType.String).Value = discordServerId;

                cmd.ExecuteNonQuery();
                return IkanoPlayerDbResult.FromSuccess();
            }
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
