using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WitcherResponsesBot.Models;
using WitcherResponsesBot.Services;

namespace WitcherResponsesBot.Bot
{
    public class ReplyWithResponsesBot
    {


        string m_botUsername;
        RedditService m_redditService;
        DateTime m_lastUpdate = DateTime.MinValue;

        readonly int POST_LIMIT = 150;

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
            Debug.LogImportant("Started Witcher ReplyToResponses bot");

            while(true)
            {
                Debug.Log("Scanning New & Hot posts for responses");

                //Scan 'New' posts
                List<Post> newPosts = m_redditService.GetNewPosts(POST_LIMIT);
                CheckPostsForResponses(newPosts);

                //Scan 'Hot' posts
                List<Post> hotPosts = m_redditService.GetHotPosts(POST_LIMIT);
                CheckPostsForResponses(hotPosts);

                //Sleep for 60 seconds
                int sleepDuration = 60 * 1000;
                Thread.Sleep(sleepDuration);
            }
        }

        /// <summary>
        /// Checks the list of posts for comments that should be replied to
        /// </summary>
        /// <param name="posts">The list of posts</param>
        void CheckPostsForResponses(List<Post> posts)
        {
            foreach (Post post in posts)
            {
                foreach (Comment comment in post.Comments)
                {
                    var containsResponseKvp = ValidateComment(comment);
                    if (containsResponseKvp.Key)
                        PostReply(comment, containsResponseKvp.Value);
                }
            }
        }

        /// <summary>
        /// Find out if the comment contains a valid voice line response
        /// </summary>
        /// <param name="comment">The body of the current comment</param>
        /// <returns>Returns kvp contains if comment is valid for the bot (Key) and the response (Value)</returns>
        KeyValuePair<bool, CharacterResponse> ValidateComment(Comment comment)
        {
            //Remove any ! and .
            string compareComment = RemoveInvalidChars(comment.Body);

            foreach (CharacterResponse response in ResponsesDatabase.Responses)
            {
                string compareResponse = RemoveInvalidChars(response.Response);

                //Check if comment has response. 
                //Only reply if the user typed the response. Not if it's inside of a normal comment
                if (compareComment == compareResponse)
                {
                    //Dont reply if bot has already replied
                    if (comment.Comments.Any(x => x.AuthorName == m_botUsername))
                        continue;
                    else
                        return new KeyValuePair<bool, CharacterResponse>(true, response);
                }
            }
            return new KeyValuePair<bool, CharacterResponse>(false, null);
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
