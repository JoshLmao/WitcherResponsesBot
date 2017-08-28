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

        public Comment ReplyToComment(Comment c, string message)
        {
            return c.Reply(message);
        }

        public List<Post> GetHotPosts(int limit)
        {
            m_activeReddit.InitOrUpdateUser();

            List<Post> hotPosts = new List<Post>();
            foreach (Subreddit sub in m_listeningSubreddits)
                hotPosts.AddRange(sub.Hot.Take(limit));
            return hotPosts;
        }

        public List<Post> GetNewPosts(int limit)
        {
            m_activeReddit.InitOrUpdateUser();

            List<Post> newPosts = new List<Post>();
            foreach (Subreddit sub in m_listeningSubreddits)
                newPosts.AddRange(sub.New.Take(limit));
            return newPosts;
        }

        public List<Post> GetStickiedPosts()
        {
            m_activeReddit.InitOrUpdateUser();

            List<Post> stickiedPosts = new List<Post>();
            foreach (Subreddit sub in m_listeningSubreddits)
                stickiedPosts.AddRange(sub.Hot.Where(x => x.IsStickied));
            return stickiedPosts;
        }
    }
}
