using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    class Debug
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static void LogError(string message, Exception ex)
        {
            Console.WriteLine(message);
        }
    }
}
