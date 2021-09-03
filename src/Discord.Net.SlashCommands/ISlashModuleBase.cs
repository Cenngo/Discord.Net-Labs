namespace Discord.SlashCommands
{
    internal interface ISlashModuleBase
    {

        void SetContext (ISlashCommandContext context);

        /// <summary>
        /// Method body to be executed before executing an application command
        /// </summary>
        /// <param name="command">Command information related to the Discord Application Command</param>
        void BeforeExecute (ExecutableInfo command);

        /// <summary>
        /// Method body to be executed after an application command execution
        /// </summary>
        /// <param name="command">Command information related to the Discord Application Command</param>
        void AfterExecute (ExecutableInfo command);

        /// <summary>
        /// Method body to be executed before the derived module is built
        /// </summary>
        /// <param name="commandService">Command Service instance that built this module</param>
        /// <param name="module">Info class of this module</param>
        void OnModuleBuilding (SlashCommandService commandService, ModuleInfo module);
    }
}