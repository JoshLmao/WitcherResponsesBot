using Fclp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitcherResponsesBot.Bot;
using WitcherResponsesBot.Services;

namespace WitcherResponsesBot
{
    class Program
    {
        static void Main(string[] args)
        {
            string redditBotUsername = null;
            string redditBotPassword = null;
            string clientId = null;
            string secretClientId = null;
            List<string> subreddits = null;

            //Setup and parse FluentCommandLineParser
            var parser = new FluentCommandLineParser();
            parser.Setup<string>('u', "username")
                .Callback(username => redditBotUsername = username)
                .WithDescription("The username of the reddit bot to post comments from")
                .Required();
            parser.Setup<string>('p', "password")
                .Callback(pass => redditBotPassword = pass)
                .WithDescription("The password to the reddit bot to post comments from")
                .Required();
            parser.Setup<string>('c', "clientId")
                .Callback(id => clientId = id)
                .WithDescription("The app client id")
                .Required();
            parser.Setup<string>('s', "secretId")
                .Callback(secret => secretClientId = secret)
                .WithDescription("The secret app client id")
                .Required();
            //ISSUE: Issue with FCLP not allowing '/' so just use Subreddit name and add /r/ below
            parser.Setup<List<string>>('r', "subreddits")
                .Callback(subs => subreddits = subs)
                .WithDescription("The list of subreddits for the bot to scan through")
                .Required();
            parser.Parse(args);

            //Parse all responses from Gamepedia
            GamepediaResponsesParser responsesParser = new GamepediaResponsesParser(Constants.CATEGORY);

            //Start bot
            ReplyWithResponsesBot responsesBot = new ReplyWithResponsesBot(redditBotUsername, redditBotPassword, clientId, secretClientId, subreddits.ToArray());
            responsesBot.Update();
        }
    }
}
