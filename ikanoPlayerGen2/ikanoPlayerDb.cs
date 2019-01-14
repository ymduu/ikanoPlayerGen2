using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    "discord_id TEXT NOT NULL)";
                    
                cmd.ExecuteNonQuery();
            }

        }

        public void AddUser(string twitterId, string discordDisplayName, string discordId)
        {
            using (var cmd = new SQLiteCommand(Connection))
            {
                cmd.CommandText = "INSERT INTO ikanoplayer_user(twitter_id, discord_display_name, discord_id) VALUES(:twitterId, :discordDisplayName, :discordId)";
                cmd.Parameters.Add("twitterId", System.Data.DbType.String).Value = twitterId;
                cmd.Parameters.Add("discordDisplayName", System.Data.DbType.String).Value = discordDisplayName;
                cmd.Parameters.Add("discordId", System.Data.DbType.String).Value = discordId;

                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
