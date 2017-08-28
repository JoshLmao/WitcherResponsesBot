using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WitcherResponsesBot.Utility
{
    class MessageModifier
    {
        /// <summary>
        /// Remove specific markdown characters to make a successful comparison and returns it
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string RemoveMarkdownCharacters(string message)
        {
            string[] formatting = new string[] { "*", "#", "^", ">", "&gt;" };
            for (int i = 0; i < formatting.Length; i++)
                message = message.Replace(formatting[i], "");
            return message;
        }

        /// <summary>
        /// Removes all unicode characters like emojis and returns it
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string RemoveUnicodeCharacters(string message)
        {
            return Regex.Replace(message, @"[^\u0000-\u007F]+", string.Empty);
        }

        /// <summary>
        /// Checks if string has a white space at start or end. If so, removes them and returns it
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string RemoveWhiteSpaceAtStartAndEnd(string message)
        {
            while (message.First() == ' ')
                message = message.Remove(0, 1);
            while (message.Last() == ' ')
                message = message.Remove(message.Length - 1, 1);
            return message;
        }
    }
}
