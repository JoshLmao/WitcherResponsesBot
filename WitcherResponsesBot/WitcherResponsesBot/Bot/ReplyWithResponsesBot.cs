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
using WitcherResponsesBot.Utility;
using System.Timers;

namespace WitcherResponsesBot.Bot
{
    public class ReplyWithResponsesBot
    {
        enum CompareType
        {
            Accurate,
            General,
        }

        string m_botUsername;
        RedditService m_redditService;
        DateTime m_botStartTime = DateTime.MinValue;
        /// <summary>
        /// Internal list to keep track of comments bot has replied to.
        /// Reason: If bot checks same comment twice, it won't be updated to include the comment it just did
        /// </summary>
        List<Comment> m_repliedToComments = new List<Comment>();
        /// <summary>
        /// The type of comparison the bot should do when comparing comments to voice lines.
        /// </summary>
        CompareType m_compareType = CompareType.General;
        System.Timers.Timer m_saveDataTimer = null;
        string m_databaseFile;

        readonly int POST_LIMIT = 75;

        public ReplyWithResponsesBot(string botUsername, string botPassword, string clientId, string clientSecretId, string[] subreddits, bool shouldRecreate, string databaseFilePath = "")
        {
            m_botUsername = botUsername;

            m_databaseFile = databaseFilePath;
            ConfigureDatabase(m_databaseFile, shouldRecreate);
            m_redditService = new RedditService(botUsername, botPassword, clientId, clientSecretId);

            //Configure subreddits to listen to
            foreach (string sub in subreddits)
            {
                m_redditService.ListenToSubreddit($"/r/{sub}");
                Debug.Log($"Listening to subreddit /r/{sub}");
            }

            m_botStartTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(m_databaseFile))
            {
                m_saveDataTimer = new System.Timers.Timer();
                m_saveDataTimer.Interval = Constants.DATABASE_SAVE_MILLISECONDS;
                m_saveDataTimer.Elapsed += OnSaveLatestDatabase;
                m_saveDataTimer.Start();
            }
        }

        void OnSaveLatestDatabase(object sender, ElapsedEventArgs e)
        {
            WriteDatabaseToFile(ResponsesDatabase.Responses, m_databaseFile);
            Debug.LogImportant("Saved latest data to database");
        }

        /// <summary>
        /// Configures the database to load all responses from file or gather the data again
        /// </summary>
        /// <param name="filePath"></param>
        void ConfigureDatabase(string filePath, bool recreateDatabase)
        {
            if (filePath != "")
            {
                if(recreateDatabase)
                {
                    Debug.Log("Transfering old database data and recreating a new one");
                    List<CharacterResponse> oldResponses = LoadResponsesFromFile(filePath);
                    
                    //Dont save to file
                    PopulateDatabase("");
                    foreach(CharacterResponse response in ResponsesDatabase.Responses)
                    {
                        var oldResponse = oldResponses.FirstOrDefault(x => x.Character == response.Character);
                        if (oldResponse != null)
                        {
                            //Properties to transfer
                            response.UseCount = oldResponse.UseCount;
                        }
                    }

                    WriteDatabaseToFile(ResponsesDatabase.Responses, filePath);
                    Debug.LogImportant("Successfully transferred old database data to new database");
                }
                else if(File.Exists(filePath))
                {
                    bool parsedSuccessfully = false;
                    try
                    {
                        ResponsesDatabase.Responses = LoadResponsesFromFile(filePath);
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
            responses = responses.OrderBy(x => x.Character).ToList();
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

            while (true)
            {
                Debug.LogImportant("Started Scanning...");

                Debug.Log("Scanning New posts...");
                List<Post> newPosts = m_redditService.GetNewPosts(POST_LIMIT);
                CheckPostsForResponses(newPosts);

                Debug.Log("Scanning Hot posts...");
                List<Post> hotPosts = m_redditService.GetHotPosts(POST_LIMIT);
                CheckPostsForResponses(hotPosts);

                //Debug.Log("Scanning Stickied posts...");
                //List<Post> stickedPosts = m_redditService.GetStickiedPosts();
                //CheckPostsForResponses(stickedPosts);

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

            switch (m_compareType)
            {
                case CompareType.Accurate:
                    return AccurateCompare(comment);
                case CompareType.General:
                    return GeneralCompare(comment);
                default:
                    throw new NotImplementedException("Need to use a comparison type that has been implemented");
            }
        }

        KeyValuePair<bool, CharacterResponse> AccurateCompare(Comment comment)
        {
            string compareComment = AccurateString(comment.Body);

            List<CharacterResponse> matchingResponses = new List<CharacterResponse>();
            foreach (CharacterResponse response in ResponsesDatabase.Responses)
            {
                string compareResponse = AccurateString(response.Response);

                //Check if comment has response. Reply if comment is solely for response
                if (compareComment == compareResponse)
                {
                    //Dont post response if phrase matched excluded phrases
                    if (Constants.EXCLUDE_PHRASES.Any(x => x.ToLower() == compareComment))
                        continue;

                    //Dont reply if bot has already replied
                    if (comment.Comments.Any(x => x.AuthorName == m_botUsername))
                        continue;
                    else
                        matchingResponses.Add(response);
                }
            }

            //Determine which is best to use from many matched
            if (matchingResponses.Count > 0)
            {
                return new KeyValuePair<bool, CharacterResponse>(true, matchingResponses.First());
            }

            return new KeyValuePair<bool, CharacterResponse>(false, null);
        }

        /// <summary>
        /// Method for an accurate comparison. Ie: The comment was wrote with the intention of a reply
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        string AccurateString(string comment)
        {
            comment = MessageModifier.RemoveWhiteSpaceAtStartAndEnd(comment);

            return comment.ToLower();
        }

        KeyValuePair<bool, CharacterResponse> GeneralCompare(Comment comment)
        {
            //Remove any ! and .
            string compareComment = GeneralString(comment.Body);
            if (string.IsNullOrEmpty(compareComment))
                return new KeyValuePair<bool, CharacterResponse>(false, null);

            List<CharacterResponse> matchingResponses = new List<CharacterResponse>();
            foreach (CharacterResponse response in ResponsesDatabase.Responses)
            {
                string compareResponse = GeneralString(response.Response);

                //If last char is punctuation in user comment, remove it and see if match
                if(MessageModifier.IsLastCharPunctuation(compareComment))
                {
                    string corrected = compareComment.Remove(compareComment.Length - 1);
                    if(corrected == compareResponse)
                    {
                        if(ValidateIfDuplicateOrExcluded(comment, compareComment, response))
                            matchingResponses.Add(response);
                    }
                }
                //Check if comment has response. Reply if comment is solely for response
                else if (compareComment == compareResponse)
                {
                    if(ValidateIfDuplicateOrExcluded(comment, compareComment, response))
                        matchingResponses.Add(response);
                }
            }

            //Determine which is best to use from many matched
            if (matchingResponses.Count > 0)
            {
                return new KeyValuePair<bool, CharacterResponse>(true, matchingResponses.First());
            }
            return new KeyValuePair<bool, CharacterResponse>(false, null);
        }

        bool ValidateIfDuplicateOrExcluded(Comment comment, string compareComment, CharacterResponse response)
        {
            //Dont post response if phrase matched excluded phrases
            if (Constants.EXCLUDE_PHRASES.Any(x => x.ToLower() == compareComment))
                return false;

            //Dont reply if bot has already replied
            if (comment.Comments.Any(x => x.AuthorName == m_botUsername))
                return false;
            else
                return true;       
        }

        /// <summary>
        /// Tries to remove as many unwanted characters as possible for a good compaison. Allows for markdown, char faces, emojis, punctuation
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        string GeneralString(string comment)
        {
            string originalString = comment;

            //Allow comparison with certain markdown formatting
            comment = MessageModifier.RemoveMarkdownCharacters(comment);

            if (string.IsNullOrEmpty(comment))
                return comment;

            string[] specificChars = new string[] { ";)", ";(", ";)", ":(", ":D", ";D" };
            for (int i = 0; i < specificChars.Length; i++)
                comment = comment.Replace(specificChars[i], "");

            //Allow comparison with other symbols
            comment = comment.Replace("!", "");
            comment = comment.Replace("?", "");
            comment = comment.Replace("'", "");
            comment = comment.Replace("?", "");
            comment = comment.Replace("\"", "");
            comment = comment.Replace(",", "");

            comment = MessageModifier.RemoveUnicodeCharacters(comment);
            if (string.IsNullOrEmpty(comment))
                return comment;

            //Remove white space last
            comment = MessageModifier.RemoveWhiteSpaceAtStartAndEnd(comment);
            if (string.IsNullOrEmpty(comment))
                return comment;

            return comment.ToLower();
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
                            "^^Got ^^a ^^question? ^^Ask ^^/u/JoshLmao ^^- ^^[Github](https://github.com/JoshLmao/WitcherResponsesBot) ^^- ^^[Suggestions/Issues](https://github.com/JoshLmao/WitcherResponsesBot/issues)";

            m_redditService.ReplyToComment(originalComment, reply);
            m_repliedToComments.Add(originalComment);

            response.UseCount++;
        }

        /// <summary>
        /// Loads previously saved database
        /// </summary>
        /// <param name="filePath">The old database file path</param>
        /// <returns></returns>
        List<CharacterResponse> LoadResponsesFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                List<CharacterResponse> loadedDatabase = JsonConvert.DeserializeObject<List<CharacterResponse>>(json);
                return loadedDatabase.OrderBy(x => x.Character).ToList();
            }
            catch(Exception e)
            {
                Debug.LogException("Unable to load existing database", e);
            }
            return null;
        }
    }
}
