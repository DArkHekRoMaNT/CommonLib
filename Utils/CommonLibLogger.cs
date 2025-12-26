using System;
using Vintagestory.API.Common;

namespace CommonLib.Utils
{
    public static class CommonLibLogger
    {
        public static ILogger? Logger { get; set; }

        public static void Error(string message)
        {
            if (Logger != null)
            {
                Logger.Error(message);
                return;
            }

            Console.Error.WriteLine(message);
        }

        public static void Error(string format, params object[] args)
        {
            if (Logger != null)
            {
                Logger.Error(format, args);
                return;
            }

            Console.Error.WriteLine(string.Format(format, args));
        }

        public static void Error(Exception exception, string format, params object[] args)
        {
            if (Logger != null)
            {
                Logger.Error(format, args);
                Logger.Error(exception);
                return;
            }

            Console.Error.WriteLine(string.Format(format, args));
            Console.Error.WriteLine(exception);
        }
    }
}
