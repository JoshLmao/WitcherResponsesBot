using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot.Services
{
    class RedditService
    {
        Reddit m_activeReddit = null;

        string m_botUsername;
        string m_botPassword;
        string m_clientId;
        string m_secretClientId;

        List<Subreddit> m_listeningSubreddits = new List<Subreddit>();

        public RedditService(string username, string password, string clientId, string clientSecretId)
        {
            m_botUsername = username;
            m_botPassword = password;
            m_clientId = clientId;
            m_secretClientId = clientSecretId;

            BotWebAgent webAgent = new BotWebAgent(m_botUsername, m_botPassword, m_clientId, m_secretClientId, "http://localhost:8080");
            m_activeReddit = new Reddit(webAgent, true);
        }

        /// <summary>
        /// Makes the manager listen to a new subreddit
        /// </summary>
        /// <param name="subreddit"></param>
        public void ListenToSubreddit(string subreddit)
        {
            if (m_activeReddit != null)
            {
                Subreddit foundSub = m_activeReddit.GetSubreddit(subreddit);
                if (foundSub != null)
                {
                    m_listeningSubreddits.Add(foundSub);
                    foundSub.Subscribe();
                }
            }
        }

        public void ReplyToComment(Comment c, string message)
        {
            c.Reply(message);
        }

        public List<Post> GetPosts()
        {
            List<Post> posts = new List<Post>();
            foreach (Subreddit sub in m_listeningSubreddits)
            {
                posts.AddRange(sub.New.Take(25));
            }
            return posts;
        }
    }
}
