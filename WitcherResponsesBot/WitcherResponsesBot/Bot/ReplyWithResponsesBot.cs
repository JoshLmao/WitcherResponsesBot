using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WitcherResponsesBot.Models;
using WitcherResponsesBot.Services;

namespace WitcherResponsesBot.Bot
{
    public class ReplyWithResponsesBot
    {
        RedditService m_redditService;

        string m_botUsername;

        public ReplyWithResponsesBot(string botUsername, string botPassword, string clientId, string clientSecretId, string[] subreddits)
        {
            m_botUsername = botUsername;

            m_redditService = new RedditService(botUsername, botPassword, clientId, clientSecretId);
            
            //Configure subreddits to listen to
            foreach(string sub in subreddits)
            {
                m_redditService.ListenToSubreddit($"/r/{sub}");
                Debug.Log($"Listening to subreddit /r/{sub}");
            }
        }

        /// <summary>
        /// Starts a infinte loop to constantly update and do it's job
        /// </summary>
        public void Update()
        {
            while(true)
            {
                var posts = m_redditService.GetPosts();
                foreach (Post post in posts)
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
            string compareComment = RemoveInvalidChars(comment.Body);

            foreach (CharacterResponse response in ResponsesDatabase.Responses)
            {
                string compareResponse = RemoveInvalidChars(response.Response);

                //Check if comment has response
                if (compareComment.Contains(compareResponse))
                {
                    //Dont reply if bot has already replied
                    if (comment.Comments.Any(x => x.AuthorName == m_botUsername))
                        continue;
                    else
                        PostReply(comment, response);
                }
            }
        }

        /// <summary>
        /// Posts a formatted reply with the voice line linking to the voice line url to a comment
        /// </summary>
        /// <param name="originalComment">The original comment</param>
        /// <param name="responseLine">The response line</param>
        void PostReply(Comment originalComment, CharacterResponse response)
        {
            Debug.Log($"Replying to '{originalComment.AuthorName}'s comment with response '{response.Character}' - '{response.Response}'");

            string reply = $"[{response.Response}]({response.Url})" +
                            Environment.NewLine +
                            "*****" +
                            Environment.NewLine +
                            "^^Got ^^a ^^question? ^^Ask ^^/u/JoshLmao ^^- ^^[Github](https://github.com/JoshLmao/WitcherResponsesBot) ^^- ^^[Suggestions](https://github.com/JoshLmao/WitcherResponsesBot/issues)";

            //Comment replyComment = originalComment.Reply(reply);
            m_redditService.ReplyToComment(originalComment, reply);
            //?
            //reply.Distinguish(VotableThing.DistinguishType.Moderator);
        }

        string RemoveInvalidChars(string originalString)
        {
            originalString = originalString.Replace("?", "");
            originalString = originalString.Replace(".", "");
            originalString = originalString.Replace("!", "");
            originalString = Regex.Replace(originalString, @"[^\u0000-\u007F]+", string.Empty);
            return originalString.ToLower();
        }
    }
}
