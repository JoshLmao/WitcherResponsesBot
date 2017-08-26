using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitcherResponsesBot
{
    class Program
    {
        static void Main(string[] args)
        {
            RedditManager rm = new RedditManager();
            rm.Update();
        }
    }
}
