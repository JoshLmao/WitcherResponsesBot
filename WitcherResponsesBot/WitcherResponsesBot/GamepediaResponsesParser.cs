using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WitcherResponsesBot.Models;

namespace WitcherResponsesBot
{
    class GamepediaResponsesParser
    {
        //Note: cmLimit max is 500   
        readonly string API_URL = "api.php?action=query&list=categorymembers&cmlimit=500''&cmprop=title&format=json&cmtitle=Category:";

        public GamepediaResponsesParser(string category)
        {
            category = category.Replace(" ", "_");
            string pageUrl = $"{Constants.BASE_URL}/{API_URL}{category}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(pageUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            if(response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                string data = readStream.ReadToEnd();

                response.Close();
                readStream.Close();

                //Do AFTER closing streams
                ParseJsonPage(data);
            }
        }

        /// <summary>
        /// Parses the json and converts it to be used
        /// </summary>
        /// <param name="json">The json from the web page</param>
        void ParseJsonPage(string json)
        {
            var deserialize = JsonConvert.DeserializeObject(json);
            JObject jsonObject = JObject.Parse(json);

            List<CharacterResponse> responses = new List<CharacterResponse>();
            foreach (var value in jsonObject["query"]["categorymembers"])
            {
                //"title":"File:Adda - Mmm\u2026 What Is It I Fancy Today\u2026.mp3"
                string fullString = value["title"].ToString();
                string withoutFile = fullString.Replace("File:", "");
                string withoutMp3 = fullString.Replace(".mp3", "");
                string withoutFileAndMp3 = withoutMp3 = withoutMp3.Replace("File:", "");

                string[] split = withoutFileAndMp3.Split(new string[] { " - " }, StringSplitOptions.None);
                string character = split[0];
                string response = split[1];

                responses.Add(new CharacterResponse(character, response, CreateMp3Url(withoutFile)));
                break; //add only Adda response for testing
            }

            ResponsesDatabase.SetDatabase(responses);
        }

        /// <summary>
        /// Creates a formatted url from the character and response string from the json
        /// </summary>
        /// <param name="charAndResponse">The character and response string (Character - Response)</param>
        /// <returns>The url for the character response</returns>
        string CreateMp3Url(string charAndResponse)
        {
            //Figure out this
            string numbers = "8/83"; 

            string urlFormat = charAndResponse.Replace(" ", "_");
            return $"{Constants.BASE_URL}/{Constants.MEDIA_URL}/{numbers}/{urlFormat}";
        }
    }
}
