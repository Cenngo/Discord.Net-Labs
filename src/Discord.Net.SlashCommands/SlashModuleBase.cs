using System;
using System.Threading.Tasks;

namespace Discord.SlashCommands
{
    /// <summary>
    /// Base class for any Slash command handling modules
    /// </summary>
    /// <typeparam name="T">Type of slash command context to be injected into the module</typeparam>
    public abstract class SlashModuleBase<T> : SlashModuleBase where T : class, ISlashCommandContext
    {
        /// <summary>
        /// Command execution context for an user interaction.
        /// </summary>
        public T Context { get; private set; }

        public override void SetContext (ISlashCommandContext context)
        {
            var newValue = context as T;
            Context = newValue ?? throw new InvalidOperationException($"Invalid context type. Expected {typeof(T).Name}, got {context.GetType().Name}.");
        }

        /// <inheritdoc cref="IDiscordInteraction.RespondAsync(string, Embed[], bool, bool, AllowedMentions, RequestOptions, MessageComponent, Embed)"/>
        protected virtual async Task RespondAsync (string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false,
            AllowedMentions allowedMentions = null, RequestOptions options = null, MessageComponent component = null, Embed embed = null) =>
            await Context.Interaction.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, component, embed).ConfigureAwait(false);

        /// <inheritdoc cref="IDiscordInteraction.FollowupAsync(string, Embed[], bool, bool, AllowedMentions, RequestOptions, MessageComponent, Embed)"/>
        protected virtual async Task<IUserMessage> FollowupAsync (string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false,
            AllowedMentions allowedMentions = null, RequestOptions options = null, MessageComponent component = null, Embed embed = null) =>
            await Context.Interaction.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, component, embed).ConfigureAwait(false);

        /// <inheritdoc cref="IMessageChannel.SendMessageAsync(string, bool, Embed, RequestOptions, AllowedMentions, MessageReference, MessageComponent)"/>
        protected virtual async Task<IUserMessage> ReplyAsync (string text = null, bool isTTS = false, Embed embed = null, RequestOptions options = null,
            AllowedMentions allowedMentions = null, MessageReference messageReference = null, MessageComponent component = null) =>
            await Context.Channel.SendMessageAsync(text, false, embed, options, allowedMentions, messageReference, component).ConfigureAwait(false);

        /// <inheritdoc cref="IDeletable.DeleteAsync(RequestOptions)"/>
        protected virtual async Task DeleteOriginalResponseAsync ( )
        {
            var response = await Context.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);
            await response.DeleteAsync().ConfigureAwait(false);
        }
    }

    public abstract class SlashModuleBase : ISlashModuleBase
    {
        internal SlashModuleBase ( ) { }

        /// <inheritdoc/>
        public virtual void AfterExecute (ExecutableInfo command) { }

        /// <inheritdoc/>
        public virtual void BeforeExecute (ExecutableInfo command) { }

        /// <inheritdoc/>
        public virtual void OnModuleBuilding (SlashCommandService commandService, ModuleInfo module) { }

        /// <inheritdoc/>
        public abstract void SetContext (ISlashCommandContext context);
    }
}
