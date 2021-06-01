using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TwitchEbooks.Twitch.Chat.Messages;

[assembly: InternalsVisibleTo("TwitchEbooks.Twitch.Tests")]
namespace TwitchEbooks.Twitch.Chat
{
    internal enum ParserState
    {
        Start,
        Tags,
        Source,
        Command,
        Parameters,
        Trailing
    }

    internal static class IrcMessageParser
    {
        public static bool TryParse(string rawMessage, out IrcMessage ircMessage)
        {
            var tags = new Dictionary<string, string>();
            string source = null, command = null;
            var parameters = new List<string>();

            var state = ParserState.Start;
            var start = 0;
            for (var i = 0; i < rawMessage.Length; i++)
            {
                switch (state, rawMessage[i])
                {
                    case (ParserState.Start, '@'):
                        // parse message tags
                        state = ParserState.Tags;
                        i++;

                        start = i;
                        string key = null;
                        for (; i < rawMessage.Length; i++)
                        {
                            if (rawMessage[i] == '=')
                                key = rawMessage[start..i];
                            else if (rawMessage[i] is ';' or ' ' && key is not null)
                                tags[key] = rawMessage[start..i];
                            else if (rawMessage[i] is ';' or ' ' && key is null)
                                tags[rawMessage[start..i]] = "1";

                            if (rawMessage[i] is '=' or ';')
                                start = i + 1;
                            else if (rawMessage[i] == ' ')
                                break;
                        }
                        break;
                    case ( < ParserState.Source, ':'):
                        state = ParserState.Source;
                        start = ++i;
                        while (i < rawMessage.Length && rawMessage[i] != ' ') i++;
                        source = rawMessage[start..i];
                        break;
                    case ( < ParserState.Command, _):
                        state = ParserState.Command;
                        start = i;
                        break;
                    case (ParserState.Command, ' '):
                        command = rawMessage[start..i];
                        state = ParserState.Parameters;
                        start = i + 1;
                        break;
                    case (ParserState.Parameters, ':'):
                        state = ParserState.Trailing;
                        start = i + 1;
                        break;
                    case (ParserState.Parameters, ' '):
                        // we've reached the end of one parameter, save it and move to the next
                        parameters.Add(rawMessage[start..i]);
                        start = i + 1;
                        break;
                }

                // if we hit the end of the message in the Parameter or Trailing states,
                // add what we have to the parameters list
                if (i >= rawMessage.Length - 1 && state >= ParserState.Parameters)
                    parameters.Add(rawMessage[start..(i + 1)]);
            }

            ircMessage = new (tags, source, command, parameters);
            return !string.IsNullOrEmpty(command);
        }
    }
}
