using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot.Models
{
    public class CharacterResponse
    {
        public string Character { get; set; }
        public string Response { get; set; }
        public string Url { get; set; }
        public uint UseCount { get; set; }

        public CharacterResponse(string character, string response, string url)
        {
            Character = character;
            Response = response;
            Url = url;
        }
    }
}
