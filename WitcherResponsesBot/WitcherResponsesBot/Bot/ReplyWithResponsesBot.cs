using Newtonsoft.Json;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
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
        DateTime m_botStartTime = DateTime.MinValue;
        /// <summary>
        /// Internal list to keep track of comments bot has replied to.
        /// Reason: If bot checks same comment twice, it won't be updated to include the comment it just did
        /// </summary>
        List<Comment> m_repliedToComments = new List<Comment>();

        readonly int POST_LIMIT = 150;

        public ReplyWithResponsesBot(string botUsername, string botPassword, string clientId, string clientSecretId, string[] subreddits, string databaseFilePath = "")
        {
            m_botUsername = botUsername;

            ConfigureDatabase(databaseFilePath);
            m_redditService = new RedditService(botUsername, botPassword, clientId, clientSecretId);

            //Configure subreddits to listen to
            foreach (string sub in subreddits)
            {
                m_redditService.ListenToSubreddit($"/r/{sub}");
                Debug.Log($"Listening to subreddit /r/{sub}");
            }

            m_botStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Configures the database to load all responses from file or gather the data again
        /// </summary>
        /// <param name="filePath"></param>
        void ConfigureDatabase(string filePath)
        {
            if (filePath != "")
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    bool parsedSuccessfully = false;

                    try
                    {
                        ResponsesDatabase.Responses = JsonConvert.DeserializeObject<List<CharacterResponse>>(json);
                        parsedSuccessfully = ResponsesDatabase.Responses != null && ResponsesDatabase.Responses.Count > 0;

                        Debug.Log($"Database file found. Using data from file {filePath}");
                    }
                    catch (Exception e)
                    {
                        parsedSuccessfully = false;
                    }

                    if (!parsedSuccessfully)
                    {
                        //If fail to parse existing database, recreate it
                        Debug.LogImportant("Couldn't deserialize previous database file. Recreating database...");
                        PopulateDatabase(filePath);
                    }
                }
                else
                {
                    Debug.LogImportant("No databasefile found. Gathering data and creating a new database file...");
                    SetupDatabaseFile(filePath);
                    PopulateDatabase(filePath);
                }
            }
            else
            {
                Debug.LogImportant("No database file path specified. Won't be able to save gathered data");
                PopulateDatabase(filePath);
            }
        }

        /// <summary>
        /// Sets up the file database to be written to once gathered the latest responses
        /// </summary>
        /// <param name="databaseFilePath">The file path of the database</param>
        void SetupDatabaseFile(string databaseFilePath)
        {
            var dir = Path.GetDirectoryName(databaseFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if(!File.Exists(databaseFilePath))
                File.Create(databaseFilePath).Close();
        }

        /// <summary>
        /// Gatheres the latest responses from the Gwent Wiki and saves them to the database and the file
        /// </summary>
        /// <param name="filePath">The file path. Can be empty if none specified</param>
        void PopulateDatabase(string filePath)
        {
            //Parse all responses from Gamepedia
            Debug.LogImportant("Starting to gather responses...");

            GamepediaResponsesParser responsesParser = new GamepediaResponsesParser(Constants.CATEGORY);
            List<CharacterResponse> responses = responsesParser.Parse();

            Debug.LogImportant("Finished gathering responses.");

            //Set responses after parsing
            ResponsesDatabase.Responses = responses;

            //Save all to file for next time
            if(filePath != "")
                WriteDatabaseToFile(responses, filePath);
        }

        /// <summary>
        /// Writes the given list of responses in json to a file
        /// </summary>
        /// <param name="responses">The current list of responses</param>
        /// <param name="filePath">The file path to save to</param>
        void WriteDatabaseToFile(List<CharacterResponse> responses, string filePath)
        {
            //Save gathered data to database instead of polling everytime on start
            string json = JsonConvert.SerializeObject(responses, Formatting.Indented);
            File.WriteAllText(filePath, json);
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

                //Sleep for x seconds
                int seconds = Constants.SLEEP_SECONDS;
                int sleepDuration = seconds * 1000;
                Debug.LogImportant($"Idling for {seconds}...");
                Thread.Sleep(sleepDuration);

                //Clear comments for next time
                m_repliedToComments.Clear();
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

                    if (comment.Comments.Count > 0)
                        CheckChildComments(comment.Comments);
                }
            }
        }

        void CheckChildComments(IList<Comment> comments)
        {
            foreach(Comment c in comments)
            {
                var containsResponseKvp = ValidateComment(c);
                if (containsResponseKvp.Key)
                    PostReply(c, containsResponseKvp.Value);

                if (c.Comments.Count > 0)
                    CheckChildComments(c.Comments);
            }
        }

        /// <summary>
        /// Find out if the comment contains a valid voice line response and matched any criteria
        /// </summary>
        /// <param name="comment">The body of the current comment</param>
        /// <returns>Returns kvp contains if comment is valid for the bot (Key) and the response (Value)</returns>
        KeyValuePair<bool, CharacterResponse> ValidateComment(Comment comment)
        {
            //Dont reply to a comment if it is before the app started. Do it if debugging
            if (comment.CreatedUTC < m_botStartTime && !System.Diagnostics.Debugger.IsAttached ||
                comment.AuthorName == m_botUsername)
                return new KeyValuePair<bool, CharacterResponse>(false, null);

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
            //Check AuthorName & Body to see if it's the same
            if (m_repliedToComments.Any(c => c.Body == originalComment.Body && c.AuthorName == originalComment.AuthorName))
                return;

            Debug.Log($"Replying to '{originalComment.AuthorName}'s comment with response '{response.Character}' - '{response.Response}'");

            string reply = $"[{response.Character}: {response.Response}]({response.Url})" +
                            Environment.NewLine +
                            "*****" +
                            Environment.NewLine +
                            "^^Got ^^a ^^question? ^^Ask ^^/u/JoshLmao ^^- ^^[Github](https://github.com/JoshLmao/WitcherResponsesBot) ^^- ^^[Suggestions](https://github.com/JoshLmao/WitcherResponsesBot/issues)";

            m_redditService.ReplyToComment(originalComment, reply);
            m_repliedToComments.Add(originalComment);
            
            //Sleep for one second after replying
            Thread.Sleep(1000);
        }

        /// <summary>
        /// Selectively remove certain characters in the response.
        /// For example: Don't allow if response is with strikethrough (~~response~~)
        /// </summary>
        /// <param name="modifiedString"></param>
        /// <returns></returns>
        string RemoveInvalidChars(string modifiedString)
        {
            string originalString = modifiedString;
            //Allow comparison with certain markdown formatting
            string[] formatting = new string[] { "*", "#", "^", ">", "&gt;" };
            for (int i = 0; i < formatting.Length; i++)
                modifiedString = modifiedString.Replace(formatting[i], "");

            string[] specificChars = new string[] { ";)", ";(", ";)", ":(", ":D", ";D" };
            for (int i = 0; i < specificChars.Length; i++)
                modifiedString = modifiedString.Replace(specificChars[i], "");

            //Allow comparison with other symbols
            modifiedString = modifiedString.Replace("!", "");
            modifiedString = modifiedString.Replace("?", "");
            modifiedString = modifiedString.Replace("'", "");
            modifiedString = modifiedString.Replace("?", "");
            modifiedString = modifiedString.Replace("\"", "");
            modifiedString = modifiedString.Replace(",", "");

            modifiedString = Regex.Replace(modifiedString, @"[^\u0000-\u007F]+", string.Empty);

            //Check if first and last aren't white spaces for correct comparison
            while(modifiedString.First() == ' ')
                modifiedString = modifiedString.Remove(0, 1);
            while (modifiedString.Last() == ' ')
                modifiedString = modifiedString.Remove(modifiedString.Length - 1, 1);

            return modifiedString.ToLower();
        }
    }
}
