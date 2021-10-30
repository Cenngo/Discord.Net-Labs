using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Discord.Interactions
{
    internal class CommandMapNode<T> where T : class, ICommandInfo
    {
        private const string RegexWildCardExp = "(?<$1>\\w+)?";

        private readonly string _openingWildCardDelimiter;
        private readonly string _closingWildCardDelimiter;
        private string _wildCardPattern => _openingWildCardDelimiter + @"(\w+)" + _closingWildCardDelimiter;
        private readonly ConcurrentDictionary<string, CommandMapNode<T>> _nodes;
        private readonly ConcurrentDictionary<string, T> _commands;
        private readonly ConcurrentDictionary<Regex, T> _wildCardCommands;

        public IReadOnlyDictionary<string, CommandMapNode<T>> Nodes => _nodes;
        public IReadOnlyDictionary<string, T> Commands => _commands;
        public IReadOnlyDictionary<Regex, T> WildCardCommands => _wildCardCommands;
        public string Name { get; }

        public CommandMapNode (string name, string openingWildCardDelimiter = null, string closingWildCardDelimiter = null)
        {
            Name = name;
            _nodes = new ConcurrentDictionary<string, CommandMapNode<T>>();
            _commands = new ConcurrentDictionary<string, T>();
            _wildCardCommands = new ConcurrentDictionary<Regex, T>();

            _openingWildCardDelimiter = openingWildCardDelimiter ?? "{";
            _closingWildCardDelimiter = closingWildCardDelimiter ?? "}";
        }

        public void AddCommand (string[] keywords, int index, T commandInfo)
        {
            if (keywords.Length == index + 1)
            {
                if (commandInfo.SupportsWildCards && Regex.IsMatch(commandInfo.Name, _wildCardPattern))
                {
                    var patternStr =  ConstructRegex(commandInfo.Name);
                    var regex = new Regex(patternStr, RegexOptions.Singleline | RegexOptions.Compiled);

                    if (!_wildCardCommands.TryAdd(regex, commandInfo))
                        throw new InvalidOperationException($"A {typeof(T).FullName} already exists with the same name: {string.Join(" ", keywords)}");
                }
                else
                {
                    if (!_commands.TryAdd(commandInfo.Name, commandInfo))
                        throw new InvalidOperationException($"A {typeof(T).FullName} already exists with the same name: {string.Join(" ", keywords)}");
                }
            }
            else
            {
                var node = _nodes.GetOrAdd(keywords[index], (key) => new CommandMapNode<T>(key, _openingWildCardDelimiter, _closingWildCardDelimiter));
                node.AddCommand(keywords, ++index, commandInfo);
            }
        }

        public bool RemoveCommand (string[] keywords, int index)
        {
            if (keywords.Length == index + 1)
                return _commands.TryRemove(keywords[index], out var _);
            else
            {
                if (!_nodes.TryGetValue(keywords[index], out var node))
                    throw new InvalidOperationException($"No descendant node was found with the name {keywords[index]}");

                return node.RemoveCommand(keywords, ++index);
            }
        }

        public SearchResult<T> GetCommand (string[] keywords, int index)
        {
            string name = string.Join(" ", keywords);

            if (keywords.Length == index + 1)
            {
                if (_commands.TryGetValue(keywords[index], out var cmd))
                    return SearchResult<T>.FromSuccess(name, cmd);
                else
                {
                    foreach (var cmdPair in _wildCardCommands)
                    {
                        var regex = cmdPair.Key;
                        var match = regex.Match(keywords[index]);

                        if (match.Success)
                        {
                            var args = new CaptureGroupResult[match.Groups.Count - 1];

                            for (var i = 1; i < match.Groups.Count; i++)
                            {
                                var group = match.Groups[i];
                                args[i - 1] = new CaptureGroupResult(regex.GroupNameFromNumber(group.Index), group.Value);
                            }

                            return SearchResult<T>.FromSuccess(name, cmdPair.Value, args.ToArray());
                        }
                    }
                }
            }
            else
            {
                if (_nodes.TryGetValue(keywords[index], out var node))
                    return node.GetCommand(keywords, ++index);
            }

            return SearchResult<T>.FromError(name, InteractionCommandError.UnknownCommand, $"No {typeof(T).FullName} found for {name}");
        }

        public SearchResult<T> GetCommand (string text, char[] seperators)
        {
            var keywords = text.Split(seperators);
            return GetCommand(keywords, 0);
        }

        private string ConstructRegex(string input) =>
            "\\A" + Regex.Replace(input, _wildCardPattern, RegexWildCardExp) + "\\Z";
    }
}
