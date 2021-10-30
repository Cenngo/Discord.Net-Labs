using Discord.Interactions.Builders;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Discord.Interactions
{
    /// <summary>
    ///     Represents the info class of an attribute based method for handling Component Interaction events
    /// </summary>
    public class ComponentCommandInfo : CommandInfo<CommandParameterInfo>
    {
        /// <inheritdoc/>
        public override IReadOnlyCollection<CommandParameterInfo> Parameters { get; }

        /// <inheritdoc/>
        public override bool SupportsWildCards => true;

        internal ComponentCommandInfo (ComponentCommandBuilder builder, ModuleInfo module, InteractionService commandService) : base(builder, module, commandService)
        {
            Parameters = builder.Parameters.Select(x => x.Build(this)).ToImmutableArray();
        }

        /// <inheritdoc/>
        public override async Task<IResult> ExecuteAsync (IInteractionCommandContext context, IServiceProvider services)
            => await ExecuteAsync(context, services, null).ConfigureAwait(false);

        /// <summary>
        ///     Execute this command using dependency injection
        /// </summary>
        /// <param name="context">Context that will be injected to the <see cref="InteractionModuleBase{T}"/></param>
        /// <param name="services">Services that will be used while initializing the <see cref="InteractionModuleBase{T}"/></param>
        /// <param name="wildCardCaptures">Provide additional string parameters to the method along with the auto generated parameters</param>
        /// <returns>
        ///     A task representing the asyncronous command execution process
        /// </returns>
        public async Task<IResult> ExecuteAsync (IInteractionCommandContext context, IServiceProvider services, IEnumerable<CaptureGroupResult> wildCardCaptures = null)
        {
            if (context.Interaction is SocketMessageComponent messageInteraction)
            {
                try
                {
                    var componentValues = messageInteraction.Data?.Values;

                    var args = new object[Parameters.Count];

                    for (var i = 0; i < wildCardCaptures?.Count(); i++)
                    {
                        var captureGroup = wildCardCaptures
                            .FirstOrDefault(x => string.Equals(x.Name, Parameters.ElementAt(i).Name, StringComparison.Ordinal));

                        if (captureGroup.Name is null)
                            return ExecuteResult.FromError(InteractionCommandError.BadArgs, "Command was invoked with too few parameters");

                        args[i] = captureGroup.Value;
                    }

                    if (componentValues is not null)
                    {
                        if(Parameters.Last().ParameterType == typeof(string[]))
                            args[args.Length - 1] = componentValues.ToArray();
                        else
                            return ExecuteResult.FromError(InteractionCommandError.BadArgs, $"Select Menu Interaction handlers must accept a {typeof(string[]).FullName} as its last parameter");
                    }

                    return await RunAsync(context, args, services).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    return ExecuteResult.FromError(ex);
                }
            }
            else
                throw new ArgumentException("Cannot execute Component Interaction handler from the provided command context");
        }

        protected override Task InvokeModuleEvent (IInteractionCommandContext context, IResult result)
            => CommandService._componentCommandExecutedEvent.InvokeAsync(this, context, result);

        protected override string GetLogString (IInteractionCommandContext context)
        {
            if (context.Guild != null)
                return $"Component Interaction: \"{base.ToString()}\" for {context.User} in {context.Guild}/{context.Channel}";
            else
                return $"Component Interaction: \"{base.ToString()}\" for {context.User} in {context.Channel}";
        }
    }
}
