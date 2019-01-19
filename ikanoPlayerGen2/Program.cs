using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Discord;

namespace ikanoPlayerGen2
{
    // refer:https://qiita.com/HAGITAKO/items/fff2e029064ea38ff13a
    // https://discord.foxbot.me/docs/guides/commands/commands.html

    class Program
    {
        public static DiscordSocketClient client;
        public static CommandService commands;
        public static IServiceProvider services;

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {

            IkanoPlayerCommands.Init();

            //IkanoPlayerCommands.GetReplyThreadBody();

            var twitterTask = Task.Run(() =>
            {
                IkanoPlayerCommands.GetReplyThreadBody();
            });

            //Discordクライアント立ち上げ
            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();
            client.MessageReceived += CommandRecieved;

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            await client.LoginAsync(TokenType.Bot, Constants.DISCORD_KEY);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private async Task CommandRecieved(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            Console.WriteLine("{0} {1}:{2}", message.Channel.Name, message.Author.Username, message);

            if (message == null) { return; }
            // コメントがユーザーかBotかの判定
            if (message.Author.IsBot) { return; }

            int argPos = 0;

            // コマンドのprefixとして!を使う
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) { return; }

            var context = new CommandContext(client, message);

            // 実行
            var result = await commands.ExecuteAsync(context, argPos, services);

            //実行できなかった場合、IkanoPlayer由来のエラーメッセージだけdiscordに投げる
            if (!result.IsSuccess) { Console.WriteLine(result.ErrorReason); }
            if (!result.IsSuccess && result is IkanoPlayerResult) { await context.Channel.SendMessageAsync(result.ErrorReason); }

        }
    }
}
