using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    public class RedditManager
    {
        Subreddit m_activeSub = null;

        readonly string CLIENT_ID = "GYlTa7nxVwOIPw";
        readonly string CLIENT_SECRET_ID = "6f9GJ6cajxQgozbRtuJUfUmqc5o";

        public RedditManager()
        {
            BotWebAgent webAgent = new BotWebAgent("WitcherResponsesBot", "testPassword312", CLIENT_ID, CLIENT_SECRET_ID, "http://localhost:8080");
            Reddit reddit = new Reddit(webAgent, true);
            m_activeSub = reddit.GetSubreddit("/r/JoshLmao");
            m_activeSub.Subscribe();
        }

        public void Update()
        {
            foreach (Post post in m_activeSub.New.Take(25))
            {
                var comment = post.Comments.FirstOrDefault(x => x.Body.Contains("Curses"));
                if (comment != null)
                {
                    var newComment = comment.Reply("[Curses!](http://google.com)");
                    newComment.Distinguish(VotableThing.DistinguishType.Moderator);
                }
            }
        }
    }
}
