namespace Discord.SlashCommands
{
    /// <summary>
    /// Holds the general information to be used when initializing <see cref="SlashCommandContext"/>
    /// </summary>
    public class SlashCommandServiceConfig
    {
        /// <summary>
        /// Log severity for the logger
        /// </summary>
        public LogSeverity LogLevel { get; } = LogSeverity.Info;
        /// <summary>
        /// Run mode that will be used when executing the commands
        /// </summary>
        public bool RunAsync { get; set; } = true;
        public bool ThrowOnError { get; set; } = true;

        /// <summary>
        /// Delimiter that will be used to seperate group names and the method name when a Message Component Interaction is recieved
        /// </summary>
        public char[] InteractionCustomIdDelimiters { get; set; } = null;

        /// <summary>
        /// Delete Interaction Acknowledgements if no interaction handler is found in the <see cref="SlashCommandService"/>
        /// </summary>
        public bool DeleteUnknownCommandAck { get; set; } = true;
    }
}