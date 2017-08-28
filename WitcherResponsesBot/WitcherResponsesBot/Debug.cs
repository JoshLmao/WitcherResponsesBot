using System;
using System.Collections.Generic;
using System.IO;
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

        static string m_logFilePath;

        /// <summary>
        /// Logs a normal message to the console
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            string formattedMessage = $"{DateTime.Now} | {message}";
            Console.WriteLine(formattedMessage);

            if (m_logFilePath != string.Empty)
                File.AppendAllText(m_logFilePath, $"{formattedMessage}{Environment.NewLine}");
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

        /// <summary>
        /// Sets the file log path to output logs
        /// </summary>
        /// <param name="logFilePath">The filepath to the log</param>
        public static void SetLoggerPath(string logFilePath)
        {
            m_logFilePath = logFilePath;

            string dir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            if(!File.Exists(m_logFilePath))
                File.Create(m_logFilePath).Close();
        }
    }
}
