using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ERModsMerger.Core;

namespace ERModsMerger
{
    internal static class DialConsole
    {
        public static void WriteLine(string text, ConsoleColor consoleColor = ConsoleColor.White)
        {
            int delayMili = 15;

            if (ModsMergerConfig.LoadedConfig != null)
                delayMili = ModsMergerConfig.LoadedConfig.ConsolePrintDelay;

            Console.ForegroundColor = consoleColor;
            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                Console.Write(chars[i]);
                Thread.Sleep(delayMili);
            }

            Console.Write("\n");
            Console.ResetColor();
        }

        public static void Write(string text, ConsoleColor consoleColor = ConsoleColor.White)
        {
            int delayMili = 15;

            if (ModsMergerConfig.LoadedConfig != null)
                delayMili = ModsMergerConfig.LoadedConfig.ConsolePrintDelay;

            Console.ForegroundColor = consoleColor;
            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                Console.Write(chars[i]);
                Thread.Sleep(delayMili);
            }
            Console.ResetColor();
        }
    }
}
