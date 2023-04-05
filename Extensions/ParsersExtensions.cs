#nullable disable

using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CommonLib.Extensions
{
    public static class ParsersExtensions
    {
        #region missing parsers

        public static LongArgParser Long(this CommandArgumentParsers parsers, string argName, long defaultValue = 0)
        {
            return new LongArgParser(argName, defaultValue, isMandatoryArg: true);
        }

        public static LongArgParser OptionalLong(this CommandArgumentParsers parsers, string argName, long defaultValue = 0)
        {
            return new LongArgParser(argName, defaultValue, isMandatoryArg: false);
        }

        public static LongArgParser LongRange(this CommandArgumentParsers parsers, string argName, long min, long max, long defaultValue = 0)
        {
            return new LongArgParser(argName, min, max, defaultValue, isMandatoryArg: true);
        }

        public static LongArgParser OptionalLongRange(this CommandArgumentParsers parsers, string argName, long min, long max, long defaultValue = 0)
        {
            return new LongArgParser(argName, min, max, defaultValue, isMandatoryArg: false);
        }

        public static FloatArgParser FloatRange(this CommandArgumentParsers parsers, float min, float max, string argName)
        {
            return new FloatArgParser(argName, min, max, isMandatoryArg: true);
        }

        public static FloatArgParser OptionalFloatRange(this CommandArgumentParsers parsers, string argName, float min, float max)
        {
            return new FloatArgParser(argName, min, max, isMandatoryArg: false);
        }

        public static DoubleArgParser OptionalDoubleRange(this CommandArgumentParsers parsers, string argName, double min, double max)
        {
            return new DoubleArgParser(argName, min, max, isMandatoryArg: false);
        }

        public static PlayerArgParser Player(this CommandArgumentParsers parsers, string argName, ICoreAPI api)
        {
            return new PlayerArgParser(argName, api, isMandatoryArg: true);
        }

        public static PlayerArgParser OptionalPlayer(this CommandArgumentParsers parsers, string argName, ICoreAPI api)
        {
            return new PlayerArgParser(argName, api, isMandatoryArg: false);
        }

        public static OnlinePlayerArgParser OptionalOnlinePlayer(this CommandArgumentParsers parsers, string argName, ICoreAPI api)
        {
            return new OnlinePlayerArgParser(argName, api, isMandatoryArg: false);
        }

        public class LongArgParser : ArgumentParserBase
        {
            private readonly long min;

            private readonly long max;

            private long value;

            private readonly long defaultValue;

            public LongArgParser(string argName, long min, long max, long defaultValue, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                this.defaultValue = defaultValue;
                this.min = min;
                this.max = max;
            }

            public override string GetSyntaxExplanation()
            {
                return "&nbsp;&nbsp;<i>" + argName + "</i> is an integer number";
            }

            public LongArgParser(string argName, long defaultValue, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                this.defaultValue = defaultValue;
                min = long.MinValue;
                max = long.MaxValue;
            }

            public override string[] GetValidRange(CmdArgs args)
            {
                return new string[2]
                {
                long.MinValue.ToString() ?? "",
                long.MaxValue.ToString() ?? ""
                };
            }

            public override object GetValue()
            {
                return value;
            }

            public override void PreProcess(TextCommandCallingArgs args)
            {
                value = defaultValue;
                base.PreProcess(args);
            }

            public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults>? onReady = null)
            {
                long? num = args.RawArgs.PopLong();
                if (!num.HasValue)
                {
                    lastErrorMessage = Lang.Get("Not a number");
                    return EnumParseResult.Bad;
                }

                if (num < min || num > max)
                {
                    lastErrorMessage = Lang.Get("Number out of range");
                    return EnumParseResult.Bad;
                }

                value = num.Value;
                return EnumParseResult.Good;
            }

            public override void SetValue(object data)
            {
                value = (long)data;
            }
        }

        public class PlayerArgParser : ArgumentParserBase
        {
            protected ICoreAPI api;

            protected IPlayer player;

            public PlayerArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                this.api = api;
            }

            public override string[] GetValidRange(CmdArgs args)
            {
                return api.World.AllPlayers.Select((IPlayer p) => p.PlayerName).ToArray();
            }

            public override object? GetValue()
            {
                return player;
            }

            public override void SetValue(object data)
            {
                player = (IPlayer)data;
            }

            public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
            {
                string playername = args.RawArgs.PopWord();
                if (playername == null)
                {
                    lastErrorMessage = Lang.Get("Argument is missing");
                    return EnumParseResult.Bad;
                }

                player = api.World.AllPlayers.FirstOrDefault((IPlayer p) => p.PlayerName == playername);
                if (player == null)
                {
                    lastErrorMessage = Lang.Get("No such player");
                }

                if (player == null)
                {
                    return EnumParseResult.Bad;
                }

                return EnumParseResult.Good;
            }
        }

        #endregion

        #region default value support

        public static FixedBoolArgParser Bool(this CommandArgumentParsers parsers, string argName, bool defaultValue = false, string trueAlias = "on")
        {
            return new FixedBoolArgParser(argName, defaultValue, trueAlias, isMandatoryArg: true);
        }

        public static FixedBoolArgParser OptionalBool(this CommandArgumentParsers parsers, string argName, bool defaultValue = false, string trueAlias = "on")
        {
            return new FixedBoolArgParser(argName, defaultValue, trueAlias, isMandatoryArg: false);
        }

        public class FixedBoolArgParser : ArgumentParserBase
        {
            private bool value;

            private bool defaultValue;

            private string trueAlias;

            public FixedBoolArgParser(string argName, bool defaultValue, string trueAlias, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                this.defaultValue = defaultValue;
                this.trueAlias = trueAlias;
            }

            public override string GetSyntaxExplanation()
            {
                return "&nbsp;&nbsp;<i>" + argName + "</i> is a boolean, including 1 or 0, yes or no, true or false, or " + trueAlias;
            }

            public override object GetValue()
            {
                return value;
            }

            public override void SetValue(object data)
            {
                value = (bool)data;
            }

            public override void PreProcess(TextCommandCallingArgs args)
            {
                value = defaultValue;
                base.PreProcess(args);
            }

            public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
            {
                bool? flag = args.RawArgs.PopBool(null, trueAlias);
                if (!flag.HasValue)
                {
                    lastErrorMessage = "Missing";
                    return EnumParseResult.Bad;
                }

                value = flag.Value;
                return EnumParseResult.Good;
            }
        }

        #endregion
    }
}
