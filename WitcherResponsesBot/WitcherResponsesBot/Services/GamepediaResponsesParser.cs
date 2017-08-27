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

namespace WitcherResponsesBot.Services
{
    class GamepediaResponsesParser
    {
        //Note: cmLimit max is 500   
        static readonly int LIMIT = 500;
        readonly string API_RESPONSES_URL = $"api.php?action=query&list=categorymembers&cmlimit={LIMIT}''&cmprop=title&format=json&cmtitle=Category:";
        string m_category;

        public GamepediaResponsesParser(string category)
        {
            m_category = category;
        }

        /// <summary>
        /// Parses all files from the specified category and saves them to the ResponsesDatabase
        /// </summary>
        public List<CharacterResponse> Parse()
        {
            string categoryReplaced = m_category.Replace(" ", "_");
            string pageUrl = $"{Constants.BASE_URL}/{API_RESPONSES_URL}{categoryReplaced}";

            //string json = GetDataFromPage(pageUrl);
            //return ParseJsonFromPage(pageUrl, json);

            return PopulateDatabase(pageUrl);
        }

        List<CharacterResponse> PopulateDatabase(string pageUrl)
        {
            List<CharacterResponse> responses = new List<CharacterResponse>();
            bool isLastPage = false;
            string previousPageContinue = "";
            string currentPageUrl = pageUrl;

            while(!isLastPage)
            {
                if(previousPageContinue != "")
                {
                    //To move to the next set of data, need to pass the cmcontinue value from last page
                    currentPageUrl = $"{pageUrl}&cmcontinue={previousPageContinue}";
                }

                string pageJson = GetDataFromPage(currentPageUrl);
                JObject jsonObject = GetJObjectFromJson(pageJson);

                if (jsonObject["continue"] != null)
                    if (jsonObject["continue"]["cmcontinue"] != null)
                        previousPageContinue = jsonObject["continue"]["cmcontinue"].ToString();
                    else
                        previousPageContinue = null;
                else
                    previousPageContinue = null;

                isLastPage = previousPageContinue == null;

                foreach (JToken value in jsonObject["query"]["categorymembers"])
                {
                    string title = value["title"].ToString();
                    string withoutFile = title.Replace("File:", "");
                    string withoutMp3 = title.Replace(".mp3", "");
                    string withoutFileAndMp3 = withoutMp3 = withoutMp3.Replace("File:", "");

                    //Remove - in between character name and response
                    string[] split = withoutFileAndMp3.Split('-');

                    string character = split[0];
                    //Remove white space that was left when removing -
                    if (character.Last() == ' ')
                        character = character.Remove(character.Length - 1, 1);

                    string response = split[1];
                    //Remove white space that was left when removing -
                    if (response.First() == ' ')
                        response = response.Remove(0, 1);

                    string url = GetUrlFromFileName(title);

                    responses.Add(new CharacterResponse(character, response, url));
                    Debug.Log($"Added New Response - {character}, {response}");
                }
            }

            return responses;
        }

        string GetDataFromPage(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
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

                return data;
            }
            return "";
        }

        /// <summary>
        /// Parses the json and converts it to be used
        /// </summary>
        /// <param name="json">The json from the web page</param>
        List<CharacterResponse> ParseJsonFromPage(string originalUrl, string json)
        {
            JObject jsonObject = GetJObjectFromJson(json);

            List<CharacterResponse> responses = new List<CharacterResponse>();
            foreach (var value in jsonObject["query"]["categorymembers"])
            {
                //Example: "title":"File:Adda - Mmm\u2026 What Is It I Fancy Today\u2026.mp3"
                string title = value["title"].ToString();
                string withoutFile = title.Replace("File:", "");
                string withoutMp3 = title.Replace(".mp3", "");
                string withoutFileAndMp3 = withoutMp3 = withoutMp3.Replace("File:", "");

                string[] split = withoutFileAndMp3.Split(new string[] { " - " }, StringSplitOptions.None);
                string character = split[0];
                string response = split[1];
                string url = GetUrlFromFileName(title);

                responses.Add(new CharacterResponse(character, response, url));
                Debug.Log($"Added New Response - {character}, {response}");
            }

            return responses;
        }

        JObject GetJObjectFromJson(string json)
        {
            return JObject.Parse(json);
        }

        /// <summary>
        /// Calls the Wiki again for the data related to that file. Parses the Url
        /// </summary>
        /// <param name="fullString"></param>
        /// <returns></returns>
        string GetUrlFromFileName(string title)
        {
            string url = $"{Constants.BASE_URL}/api.php?action=query&titles={title}&prop=imageinfo&iiprop=url&format=json";
            string json = GetDataFromPage(url);
            JObject jsonObject = GetJObjectFromJson(json);

            try
            {
                //Full path of json on each media page Query -> Pages -> Id of file -> ImageInfo -> Url
                //Example url with formatted data displayed: 
                //https://gwent.gamepedia.com/api.php?action=query&titles=File:Adda%20-%20Mmm%E2%80%A6%20What%20Is%20It%20I%20Fancy%20Today%E2%80%A6.mp3&prop=imageinfo&iiprop=url
                foreach (var page in jsonObject["query"]["pages"])
                {
                    foreach(var child in page)
                        foreach(var image in child["imageinfo"])
                            return image["url"].ToString();
                }                
            }
            catch (Exception e)
            {
                Debug.LogException($"Unable to load url for page '{title}'", e);
            }

            return "";
        }
    }
}
