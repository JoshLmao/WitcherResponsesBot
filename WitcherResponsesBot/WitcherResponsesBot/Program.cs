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
            string databaseFilePath = "";
            string logFilePath = "";

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
            parser.Setup<string>('f', "databaseFilePath")
                .Callback(path => databaseFilePath = path)
                .WithDescription("The file path to where to save the database file");
            parser.Setup<string>('l', "log")
                .Callback(log => logFilePath = log)
                .WithDescription("The file path of where to save the log file");
            parser.Parse(args);

            if (logFilePath != string.Empty)
                Debug.SetLoggerPath(logFilePath);

            Debug.LogImportant("Application Started");
            ReplyWithResponsesBot responsesBot = new ReplyWithResponsesBot(redditBotUsername, redditBotPassword, clientId, secretClientId, subreddits.ToArray(), databaseFilePath);
            try
            {
                responsesBot.Update();
            }
            catch(Exception e)
            {
                Debug.LogException("Application Crashed", e);
            }
        }
    }
}
