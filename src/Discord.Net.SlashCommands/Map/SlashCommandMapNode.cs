using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Discord.SlashCommands
{
    internal class SlashCommandMapNode<T> where T : class, ICommandInfo
    {
        private const string RegexWildCardExp = "(\\w+)?";

        private readonly string _wildCardStr = "*";
        private ConcurrentDictionary<string, SlashCommandMapNode<T>> _nodes;
        private ConcurrentDictionary<string, T> _commands;
        private ConcurrentDictionary<Regex, T> _wildCardCommands;

        public IReadOnlyDictionary<string, SlashCommandMapNode<T>> Nodes => _nodes;
        public IReadOnlyDictionary<string, T> Commands => _commands;
        public IReadOnlyDictionary<Regex, T> WildCardCommands => _wildCardCommands;
        public string Name { get; }

        public SlashCommandMapNode (string name, string wildCardExp = null)
        {
            Name = name;
            _nodes = new ConcurrentDictionary<string, SlashCommandMapNode<T>>();
            _commands = new ConcurrentDictionary<string, T>();
            _wildCardCommands = new ConcurrentDictionary<Regex, T>();

            if (!string.IsNullOrEmpty(wildCardExp))
                _wildCardStr = wildCardExp;
        }

        public void AddCommand (string[] keywords, int index, T commandInfo)
        {
            if (keywords.Length == index + 1)
            {
                if (commandInfo.SupportsWildCards && commandInfo.Name.Contains(_wildCardStr))
                {
                    var patternStr = commandInfo.Name.Replace(_wildCardStr, RegexWildCardExp);
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
                var node = _nodes.GetOrAdd(keywords[index], (key) => new SlashCommandMapNode<T>(key, _wildCardStr));
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

            if(keywords.Length == index + 1)
            {
                if (_commands.TryGetValue(keywords[index], out var cmd))
                    return SearchResult<T>.FromSuccess(name, cmd);
                else
                {
                    foreach(var cmdPair in _wildCardCommands)
                    {
                        var match = cmdPair.Key.Match(keywords[index]);
                        if (match.Success && match.Value.Length == keywords[index].Length)
                        {
                            var args = new string[match.Groups.Count - 1];

                            for (var i = 1; i < match.Groups.Count; i++)
                                args[i - 1] = match.Groups[i].Value;

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

            return SearchResult<T>.FromError(name, SlashCommandError.UnknownCommand, $"No {typeof(T).FullName} found for {name}");
        }

        public SearchResult<T> GetCommand (string text, int index, char[] seperators)
        {
            var keywords = text.Split(seperators);
            return GetCommand(keywords, index);
        }
    }
}
