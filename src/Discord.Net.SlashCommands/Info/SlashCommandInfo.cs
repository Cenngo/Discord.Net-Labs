using Discord.SlashCommands.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    /// Provides the information of a Slash Command
    /// </summary>
    public class SlashCommandInfo : IExecutableInfo
    {
        private readonly Func<ISlashCommandContext, object[], IServiceProvider, SlashCommandInfo, Task> _action;

        /// <summary>
        /// <see cref="SlashCommandService"/> this command belongs to
        /// </summary>
        public SlashCommandService CommandService { get; }

        /// <summary>
        /// Get the name of this command that will be used to both execute and register this command
        /// </summary>
        public string Name { get; }
        public bool IsWildCard { get; } = false;
        /// <summary>
        /// Get the description that will be shown in Discord
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// Whether this command is executable by default
        /// </summary>
        public bool DefaultPermission { get; }
        /// <summary>
        /// Get the information on Parameters that belong to this command
        /// </summary>
        public IReadOnlyList<SlashParameterInfo> Parameters { get; }
        /// <summary>
        /// Module this commands belongs to
        /// </summary>
        public SlashModuleInfo Module { get; }
        /// <summary>
        /// Information on this commands group, if it has one
        /// </summary>
        public SlashGroupInfo Group { get; }
        /// <summary>
        /// Get the list of attributes of this command
        /// </summary>
        public IReadOnlyList<Attribute> Attributes { get; }

        internal SlashCommandInfo (Builders.SlashCommandBuilder builder, SlashModuleInfo module, SlashCommandService commandService)
        {
            CommandService = commandService;
            Module = module;

            Name = builder.Name;
            Description = builder.Description;
            Group = builder.Group;
            DefaultPermission = builder.DefaultPermission;
            Parameters = builder.Parameters.Select(x => x.Build(this)).ToImmutableArray();
            Attributes = builder.Attributes.ToImmutableArray();

            _action = builder.Callback;
        }

        /// <inheritdoc/>
        public async Task<IResult> ExecuteAsync (ISlashCommandContext context, IServiceProvider services)
        {
            if (context.Interaction is SocketSlashCommand commandInteraction)
            {
                var options = commandInteraction.Data.Options;

                IList<SocketSlashCommandDataOption> args = options?.ToList();
                while (args != null && args.Any(x => x?.Type == ApplicationCommandOptionType.SubCommand || x?.Type == ApplicationCommandOptionType.SubCommandGroup))
                    args = args.ElementAt(0)?.Options?.ToList();

                return await ExecuteAsync(context, Parameters, args, services);
            }
            else
                return ExecuteResult.FromError(SlashCommandError.ParseFailed, $"Provided {nameof(ISlashCommandContext)} belongs to a message component");
        }

        public async Task<IResult> ExecuteAsync (ISlashCommandContext context, IEnumerable<SlashParameterInfo> paramList,
            IEnumerable<SocketSlashCommandDataOption> argList, IServiceProvider services)
        {
            services = services ?? EmptyServiceProvider.Instance;

            try
            {
                object[] args = await GenerateArgs(context, paramList, argList, services).ConfigureAwait(false);

                if (CommandService._runAsync)
                {
                    _ = Task.Run(async ( ) =>
                    {
                        await ExecuteInternalAsync(context, args, services).ConfigureAwait(false);
                    });
                }
                else
                    return await ExecuteInternalAsync(context, args, services).ConfigureAwait(false);

                return ExecuteResult.FromSuccess();
            }
            catch (Exception ex)
            {
                return ExecuteResult.FromError(ex);
            }
        }

        private async Task<IResult> ExecuteInternalAsync (ISlashCommandContext context, object[] args, IServiceProvider services)
        {
            await Module.CommandService._cmdLogger.DebugAsync($"Executing {GetLogString(context)}").ConfigureAwait(false);

            try
            {
                var task = _action(context, args, services, this);

                if (task is Task<IResult> resultTask)
                {
                    var result = await resultTask.ConfigureAwait(false);
                    await Module.CommandService._commandExecutedEvent.InvokeAsync(this, context, result).ConfigureAwait(false);
                    if (result is RuntimeResult || result is ExecuteResult)
                        return result;
                }
                else
                {
                    await task.ConfigureAwait(false);
                    var result = ExecuteResult.FromSuccess();
                    await Module.CommandService._commandExecutedEvent.InvokeAsync(this, context, result).ConfigureAwait(false);
                    return result;
                }


                return ExecuteResult.FromError(SlashCommandError.Unsuccessful, "Command execution failed for an unknown reason");
            }
            catch (Exception ex)
            {
                var originalEx = ex;
                while (ex is TargetInvocationException)
                    ex = ex.InnerException;

                await Module.CommandService._cmdLogger.ErrorAsync(ex);

                var result = ExecuteResult.FromError(ex);
                await Module.CommandService._commandExecutedEvent.InvokeAsync(this, context, result).ConfigureAwait(false);

                if (Module.CommandService._throwOnError)
                {
                    if (ex == originalEx)
                        throw;
                    else
                        ExceptionDispatchInfo.Capture(ex).Throw();
                }

                return result;
            }
            finally
            {
                await Module.CommandService._cmdLogger.VerboseAsync($"Executed {GetLogString(context)}").ConfigureAwait(false);
            }
        }

        private async Task<object[]>  GenerateArgs (ISlashCommandContext context, IEnumerable<SlashParameterInfo> paramList,
            IEnumerable<SocketSlashCommandDataOption> options, IServiceProvider services)
        {
            if (paramList?.Count() < options?.Count())
                throw new InvalidOperationException("Command was invoked with too many parameters");

            var result = new List<object>();

            foreach (var parameter in paramList)
            {
                var arg = options?.FirstOrDefault(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

                if (arg == null || arg == default)
                {
                    if (parameter.IsRequired)
                        throw new InvalidOperationException("Command was invoked with too few parameters");
                    else
                        result.Add(Type.Missing);
                }
                else
                {
                    var typeReader = parameter.TypeReader;

                    if (!typeReader.CanConvertTo(parameter.ParameterType))
                        throw new InvalidOperationException($"Type {nameof(parameter.ParameterType)} cannot be read by the registered Type Reader");

                    var readResult = await typeReader.ReadAsync(context, arg, services).ConfigureAwait(false);

                    if (!readResult.IsSuccess)
                        throw new InvalidOperationException($"Argument Read was not successful: {readResult.ErrorReason}");

                    result.Add(readResult.Value);
                }
            }

            return result.ToArray();
        }

        private string GetLogString (ISlashCommandContext context)
        {
            if (context.Guild != null)
                return $"\"{Name}\" for {context.User} in {context.Guild}/{context.Channel}";
            else
                return $"\"{Name}\" for {context.User} in {context.Channel}";
        }
    }
}
