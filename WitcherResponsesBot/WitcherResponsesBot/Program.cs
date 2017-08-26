using Fclp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .Required();
            parser.Setup<string>('p', "password")
                .Callback(pass => redditBotPassword = pass)
                .Required();
            parser.Setup<string>('c', "clientId")
                .Callback(id => clientId = id)
                .Required();
            parser.Setup<string>('s', "secretId")
                .Callback(secret => secretClientId = secret)
                .Required();
            //READ: Issue with FCLP not allowing '/' so just use Subreddit name
            parser.Setup<List<string>>('r', "subreddits")
                .Callback(subs => subreddits = subs)
                .Required();
            parser.Parse(args);

            RedditManager rm = new RedditManager(redditBotUsername, redditBotPassword, clientId, secretClientId);
            foreach (string sub in subreddits)
                rm.ListenToSubreddit($"/r/{sub}");

            rm.Update();
        }
    }
}
