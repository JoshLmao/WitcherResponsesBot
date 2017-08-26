using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    public class ResponsesDatabase
    {
        /// <summary>
        /// Dictionary of formatted voice lines & their URL
        /// </summary>
        public static Dictionary<string, string> Responses = new Dictionary<string, string>()
        {
            { "Curses!", "https://google.com" }
        };

        public static string GetVoiceLineUrl(string voiceLine)
        {
            string lower = voiceLine.ToLower();
            if (Responses.ContainsKey(lower))
                return Responses[lower];
            else
                return string.Empty;
        }
    }
}
