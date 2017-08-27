using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    class Debug
    {
        static ConsoleColor m_errorColor = ConsoleColor.Red;
        static ConsoleColor m_importantColor = ConsoleColor.Magenta;
        static ConsoleColor m_defaultColor = ConsoleColor.White;

        /// <summary>
        /// Logs a normal message to the console
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            Console.WriteLine($"{DateTime.Now} | {message}");
        }

        /// <summary>
        /// Logs an error message to the console. Shows red
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public static void LogException(string message, Exception ex)
        {
            Console.ForegroundColor = m_errorColor;
            Log(message);
            Console.ForegroundColor = m_defaultColor;
        }

        public static void LogImportant(string message)
        {
            Console.ForegroundColor = m_importantColor;
            Log(message);
            Console.ForegroundColor = m_defaultColor;
        }
    }
}
