using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    public class Constants
    {
        public static readonly string BASE_URL = "https://gwent.gamepedia.com";
        public static readonly string MEDIA_URL = "media/gwent.gamepedia.com";
        public static readonly string CATEGORY = "Audio";

        /// <summary>
        /// Amount of seconds to sleep inbetween an update
        /// </summary>
        public static int SLEEP_SECONDS = 60;

        public static readonly List<string> EXCLUDE_PHRASES = new List<string>()
        {
            "Yes", "No", "Ok", "Okay", "Uh", "Grrr"
        };

        public static int DATABASE_SAVE_MILLISECONDS = (60 * 60) * 1000;
    }
}
