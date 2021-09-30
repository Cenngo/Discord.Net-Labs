using System;
using System.Collections.Generic;
using System.Linq;

namespace Discord.Interactions
{
    internal static class ApplicationCommandRestUtil
    {
        #region Parameters
        public static ApplicationCommandOptionProperties ToApplicationCommandOptionProps (this SlashCommandParameterInfo parameterInfo)
        {
            var props = new ApplicationCommandOptionProperties
            {
                Name = parameterInfo.Name,
                Description = parameterInfo.Description,
                Type = parameterInfo.DiscordOptionType,
                Required = parameterInfo.IsRequired,
                Choices = parameterInfo.Choices?.Select(x => new ApplicationCommandOptionChoiceProperties
                {
                    Name = x.Name,
                    Value = x.Value
                })?.ToList()
            };
            parameterInfo.TypeConverter.Write(props, parameterInfo);

            return props;
        }
        #endregion

        #region Commands

        public static ApplicationCommandProperties ToApplicationCommandProps (this SlashCommandInfo commandInfo) =>
            new SlashCommandProperties
            {
                Name = commandInfo.Name,
                Description = commandInfo.Description,
                DefaultPermission = commandInfo.DefaultPermission,
                Options = commandInfo.Parameters.Select(x => x.ToApplicationCommandOptionProps())?.ToList() ?? Optional<List<ApplicationCommandOptionProperties>>.Unspecified
            };

        public static ApplicationCommandOptionProperties ToApplicationCommandOptionProps (this SlashCommandInfo commandInfo) =>
            new ApplicationCommandOptionProperties
            {
                Name = commandInfo.Name,
                Description = commandInfo.Description,
                Type = ApplicationCommandOptionType.SubCommand,
                Required = false,
                Options = commandInfo.Parameters?.Select(x => x.ToApplicationCommandOptionProps())?.ToList()
            };

        public static ApplicationCommandProperties ToApplicationCommandProps (this ContextCommandInfo commandInfo) =>
            new ContextCommandProperties(commandInfo.CommandType)
            {
                Name = commandInfo.Name,
                DefaultPermission = commandInfo.DefaultPermission
            };
        #endregion

        #region Modules

        public static IReadOnlyCollection<ApplicationCommandProperties> ToApplicationCommandProps (this ModuleInfo moduleInfo, bool ignoreDontRegister = false)
        {
            var args = new List<ApplicationCommandProperties>();

            moduleInfo.ParseModuleModel(args, ignoreDontRegister);
            return args;
        }

        private static void ParseModuleModel (this ModuleInfo moduleInfo, List<ApplicationCommandProperties> args, bool ignoreDontRegister)
        {
            if (moduleInfo.DontAutoRegister && !ignoreDontRegister)
                return;

            args.AddRange(moduleInfo.ContextCommands?.Select(x => x.ToApplicationCommandProps()));

            if (!moduleInfo.IsSlashGroup)
            {
                args.AddRange(moduleInfo.SlashCommands?.Select(x => x.ToApplicationCommandProps()));

                foreach (var submodule in moduleInfo.SubModules)
                    submodule.ParseModuleModel(args, ignoreDontRegister);
            }
            else
            {
                var options = new List<ApplicationCommandOptionProperties>();

                foreach (var command in moduleInfo.SlashCommands)
                {
                    if (command.IgnoreGroupNames)
                        args.Add(command.ToApplicationCommandProps());
                    else
                        options.Add(command.ToApplicationCommandOptionProps());
                }

                options.AddRange(moduleInfo.SubModules?.SelectMany(x => x.ParseSubModule(args, ignoreDontRegister)));

                args.Add(new SlashCommandProperties
                {
                    Name = moduleInfo.SlashGroupName.ToLower(),
                    Description = moduleInfo.Description,
                    DefaultPermission = moduleInfo.DefaultPermission,
                    Options = options
                });
            }
        }

        private static IReadOnlyCollection<ApplicationCommandOptionProperties> ParseSubModule (this ModuleInfo moduleInfo, List<ApplicationCommandProperties> args,
            bool ignoreDontRegister)
        {
            if (moduleInfo.DontAutoRegister && !ignoreDontRegister)
                return Array.Empty<ApplicationCommandOptionProperties>();

            args.AddRange(moduleInfo.ContextCommands?.Select(x => x.ToApplicationCommandProps()));

            var options = new List<ApplicationCommandOptionProperties>();
            options.AddRange(moduleInfo.SubModules?.SelectMany(x => x.ParseSubModule(args, ignoreDontRegister)));

            foreach (var command in moduleInfo.SlashCommands)
            {
                if (command.IgnoreGroupNames)
                    args.Add(command.ToApplicationCommandProps());
                else
                    options.Add(command.ToApplicationCommandOptionProps());
            }

            if (!moduleInfo.IsSlashGroup)
                return options;
            else
                return new List<ApplicationCommandOptionProperties>() { new ApplicationCommandOptionProperties
                {
                    Name = moduleInfo.SlashGroupName.ToLower(),
                    Description = moduleInfo.Description,
                    Type = ApplicationCommandOptionType.SubCommandGroup,
                    Options = options
                } };
        }

        #endregion

        public static ApplicationCommandProperties ToApplicationCommandProps (this IApplicationCommand command)
        {
            switch (command.Type)
            {
                case ApplicationCommandType.Slash:
                    return new SlashCommandProperties
                    {
                        Name = command.Name,
                        Description = command.Description,
                        DefaultPermission = command.DefaultPermission,
                        Options = command.Options?.Select(x => x.ToApplicationCommandOptionProps())?.ToList() ?? Optional<List<ApplicationCommandOptionProperties>>.Unspecified
                    };
                case ApplicationCommandType.User:
                case ApplicationCommandType.Message:
                    return new ContextCommandProperties(command.Type)
                    {
                        Name = command.Name,
                        DefaultPermission = command.DefaultPermission
                    };
                default:
                    throw new InvalidOperationException($"Cannot create command properties for command type {command.Type}");
            }
        }

        public static ApplicationCommandOptionProperties ToApplicationCommandOptionProps (this IApplicationCommandOption commandOption) =>
            new ApplicationCommandOptionProperties
            {
                Name = commandOption.Name,
                Description = commandOption.Description,
                Type = commandOption.Type,
                Required = commandOption.Required,
                Choices = commandOption.Choices?.Select(x => new ApplicationCommandOptionChoiceProperties
                {
                    Name = x.Name,
                    Value = x.Value
                }).ToList(),
                Options = commandOption.Options?.Select(x => x.ToApplicationCommandOptionProps()).ToList()
            };
    }

    internal sealed class ContextCommandProperties : ApplicationCommandProperties
    {
        internal override ApplicationCommandType Type { get; }

        public ContextCommandProperties (ApplicationCommandType type)
        {
            Type = type;
        }
    }
}