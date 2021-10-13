using Discord.WebSocket;

namespace Discord.Interactions
{
    /// <summary>
    /// The sharded variant of <see cref="SocketInteractionCommandContext"/>
    /// </summary>
    public class ShardedInteractionCommandContext : SocketInteractionCommandContext, IInteractionCommandContext
    {
        /// <summary>
        /// Get the <see cref="DiscordSocketClient"/> that the command will be executed with
        /// </summary>
        public new DiscordShardedClient Client { get; }

        /// <summary>
        /// Initializes a <see cref="ShardedInteractionCommandContext"/>
        /// </summary>
        /// <param name="client">The underlying client.</param>
        /// <param name="interaction">The underlying interaction</param>
        public ShardedInteractionCommandContext (DiscordShardedClient client, SocketInteraction interaction)
            : base(client.GetShard(GetShardId(client, ( interaction.User as SocketGuildUser )?.Guild)), interaction)
        {
            Client = client;
        }

        private static int GetShardId (DiscordShardedClient client, IGuild guild)
            => guild == null ? 0 : client.GetShardIdFor(guild);
    }
}