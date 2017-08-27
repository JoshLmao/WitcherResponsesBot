using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitcherResponsesBot.Models;

namespace WitcherResponsesBot.Services
{
    public class ResponsesDatabase
    {
        /// <summary>
        /// Dictionary of formatted voice lines & their URL
        /// </summary>
        public static List<CharacterResponse> Responses = null;

        public static void SetDatabase(List<CharacterResponse> database)
        {
            Responses = database;
        }
    }
}
