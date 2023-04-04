using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CommonLib.Extensions
{
    public static class ParsersExtensions
    {
        public static LongArgParser Long(this CommandArgumentParsers parsers, string argName)
        {
            return new LongArgParser(argName, 0, isMandatoryArg: true);
        }

        public static LongArgParser OptionalLong(this CommandArgumentParsers parsers, string argName)
        {
            return new LongArgParser(argName, 0, isMandatoryArg: false);
        }

        public static LongArgParser LongRange(this CommandArgumentParsers parsers, string argName, long min, long max)
        {
            return new LongArgParser(argName, min, max, 0, isMandatoryArg: true);
        }

        public static LongArgParser OptionalLongRange(this CommandArgumentParsers parsers, string argName, long min, long max)
        {
            return new LongArgParser(argName, min, max, 0, isMandatoryArg: false);
        }

        public static FloatArgParser Float(this CommandArgumentParsers parsers, string argName)
        {
            return new FloatArgParser(argName, isMandatoryArg: true);
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

        public class LongArgParser : ArgumentParserBase
        {
            private readonly long _min;

            private readonly long _max;

            private long _value;

            private readonly long _defaultValue;

            public LongArgParser(string argName, long min, long max, long defaultValue, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                _defaultValue = defaultValue;
                _min = min;
                _max = max;
            }

            public override string GetSyntaxExplanation()
            {
                return "&nbsp;&nbsp;<i>" + argName + "</i> is an integer number";
            }

            public LongArgParser(string argName, long defaultValue, bool isMandatoryArg)
                : base(argName, isMandatoryArg)
            {
                _defaultValue = defaultValue;
                _min = long.MinValue;
                _max = long.MaxValue;
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
                return _value;
            }

            public override void PreProcess(TextCommandCallingArgs args)
            {
                _value = _defaultValue;
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

                if (num < _min || num > _max)
                {
                    lastErrorMessage = Lang.Get("Number out of range");
                    return EnumParseResult.Bad;
                }

                _value = num.Value;
                return EnumParseResult.Good;
            }

            public override void SetValue(object data)
            {
                _value = (long)data;
            }
        }

        public class PlayerArgParser : ArgumentParserBase
        {
            protected ICoreAPI api;

            protected IPlayer? player;

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
                    lastErrorMessage = Lang.Get("No such player online");
                }

                if (player == null)
                {
                    return EnumParseResult.Bad;
                }

                return EnumParseResult.Good;
            }
        }
    }
}
