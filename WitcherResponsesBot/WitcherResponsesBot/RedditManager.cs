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
        Reddit m_activeReddit = null;

        string m_botUsername;
        string m_botPassword;
        string m_clientId;
        string m_secretClientId;

        List<Subreddit> m_listeningSubreddits = new List<Subreddit>();

        public RedditManager(string username, string password, string clientId, string clientSecretId)
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
            if(m_activeReddit != null)
            {
                Subreddit foundSub = m_activeReddit.GetSubreddit(subreddit);
                if (foundSub != null)
                {
                    m_listeningSubreddits.Add(foundSub);
                    foundSub.Subscribe();
                }
            }   
        }
        
        public void Update()
        {
            //Search all subs
            foreach(Subreddit sub in m_listeningSubreddits)
            {
                //Search posts
                foreach (Post post in sub.New.Take(25))
                {
                    //Search comments
                    foreach (Comment comment in post.Comments)
                        ValidateComment(comment);
                }
            }

        }

        /// <summary>
        /// Find out if the comment contains a valid voice line response
        /// </summary>
        /// <param name="comment">The body of the current comment</param>
        void ValidateComment(Comment comment)
        {
            //Remove any ! and .
            string replacedComment = comment.Body.Replace("!", "");
            replacedComment = comment.Body.Replace(".", "");

            foreach (string voiceLine in ResponsesDatabase.Responses.Keys)
            {
                //Original comment contains the response
                if (comment.Body.Contains(replacedComment))
                {
                    ////Dont reply if bot has already replied
                    if (comment.Comments.Any(x => x.AuthorName == m_botUsername))
                        continue;
                    else
                        PostReply(comment, voiceLine);
                }
            }
        }

        /// <summary>
        /// Posts a formatted reply with the voice line linking to the voice line url to a comment
        /// </summary>
        /// <param name="originalComment">The original comment</param>
        /// <param name="responseLine">The response line</param>
        void PostReply(Comment originalComment, string responseLine)
        {
            var reply = originalComment.Reply($"[{responseLine}]({ResponsesDatabase.GetVoiceLineUrl(responseLine)})");
            //?
            //reply.Distinguish(VotableThing.DistinguishType.Moderator);
        }
    }
}
