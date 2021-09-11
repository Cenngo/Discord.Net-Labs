using Discord.SlashCommands.Builders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Discord.SlashCommands
{
    /// <summary>
    /// Contains the information of a Slash command module
    /// </summary>
    public class ModuleInfo
    {
        /// <summary>
        /// Command service this module belongs to
        /// </summary>
        public SlashCommandService CommandService { get; }

        /// <summary>
        /// Get the name of this module class
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get the name of this module that will be displayed on Discord
        /// </summary>
        /// <remarks>
        /// This value may be missing if the commands are registered as standalone
        /// </remarks>
        public string SlashGroupName { get; }

        /// <summary>
        /// Whether this module has a <see cref="SlashGroupAttribute"/> or not
        /// </summary>
        public bool IsSlashGroup => !string.IsNullOrEmpty(SlashGroupName);

        /// <summary>
        /// Description of this module
        /// </summary>
        /// <remarks>
        /// This value may be missing if the commands are registered as standalone
        /// </remarks>
        public string Description { get; }

        /// <summary>
        /// Check if the Application Command for this module can be executed by default
        /// </summary>
        /// <remarks>
        /// If this module does not have a <see cref="SlashGroupAttribute"/> or it has a parent that does, this property will be unaffective
        /// </remarks>
        public bool DefaultPermission { get; }

        /// <summary>
        /// Get the collection of Sub Modules of this module
        /// </summary>
        public IReadOnlyList<ModuleInfo> SubModules { get; }

        /// <summary>
        /// Get the information list of the Slash Commands that belong to this module
        /// </summary>
        public IReadOnlyList<SlashCommandInfo> SlashCommands { get; }

        /// <summary>
        /// Get the information list of the Context Commands that belong to this module
        /// </summary>
        public IReadOnlyList<ContextCommandInfo> ContextCommands { get;
        }
        /// <summary>
        /// Get the information list of the Message Component handlers that belong to this module
        /// </summary>
        public IReadOnlyCollection<InteractionInfo> Interactions { get; }

        /// <summary>
        /// Get the declaring type of this module, if this is a Sub Module
        /// </summary>
        public ModuleInfo Parent { get; }

        /// <summary>
        /// <see langword="true"/> if this module is declared under another <see cref="SlashModuleBase{T}"/>
        /// </summary>
        public bool IsSubModule => Parent != null;

        /// <summary>
        /// Get a collection of the attributes of this module
        /// </summary>
        public IReadOnlyCollection<Attribute> Attributes { get; }

        /// <summary>
        /// Get a collection of the preconditions of this module
        /// </summary>
        public IReadOnlyCollection<PreconditionAttribute> Preconditions { get; }

        /// <summary>
        /// <see langword="true"/> if this module has a valid Group name and represent a top level application command in Discord
        /// </summary>
        public bool IsTopLevel { get; }

        internal ModuleInfo (ModuleBuilder builder, SlashCommandService commandService = null,  ModuleInfo parent = null)
        {
            CommandService = commandService ?? builder.CommandService;

            Name = builder.Name;
            SlashGroupName = builder.SlashGroupName;
            Description = builder.Description;
            Parent = parent;
            DefaultPermission = builder.DefaultPermission;
            SlashCommands = BuildSlashCommands(builder).ToImmutableArray();
            ContextCommands = BuildContextCommands(builder).ToImmutableArray();
            Interactions = BuildInteractions(builder).ToImmutableArray();
            SubModules = BuildSubModules(builder).ToImmutableArray();;
            Attributes = BuildAttributes(builder).ToImmutableArray();
            Preconditions = BuildPreconditions(builder).ToImmutableArray();
            IsTopLevel = ( parent == null || !parent.IsTopLevel ) && IsSlashGroup;

            if (IsSlashGroup)
            {
                Discord.Preconditions.SlashCommandName(SlashGroupName, nameof(SlashGroupName));
                Discord.Preconditions.SlashCommandDescription(Description, nameof(Description));
            }
        }

        private IEnumerable<ModuleInfo> BuildSubModules(ModuleBuilder builder, SlashCommandService commandService = null)
        {
            var result = new List<ModuleInfo>();

            foreach (Builders.ModuleBuilder moduleBuilder in builder.SubModules)
                result.Add(moduleBuilder.Build(commandService, this));

            return result;
        }

        private IEnumerable<SlashCommandInfo> BuildSlashCommands (ModuleBuilder builder)
        {
            var result = new List<SlashCommandInfo>();

            foreach (Builders.SlashCommandBuilder commandBuilder in builder.SlashCommands)
                result.Add(commandBuilder.Build(this, CommandService));

            return result;
        }

        private IEnumerable<ContextCommandInfo> BuildContextCommands(ModuleBuilder builder)
        {
            var result = new List<ContextCommandInfo>();

            foreach (Builders.ContextCommandBuilder commandBuilder in builder.ContextCommands)
                result.Add(commandBuilder.Build(this, CommandService));

            return result;
        }

        private IEnumerable<InteractionInfo> BuildInteractions(ModuleBuilder builder)
        {
            var result = new List<InteractionInfo>();

            foreach (var interactionBuilder in builder.Interactions)
                result.Add(interactionBuilder.Build(this, CommandService));

            return result;
        }

        private IEnumerable<Attribute> BuildAttributes (ModuleBuilder builder)
        {
            var result = new List<Attribute>();
            var currentParent = builder;

            while (currentParent != null)
            {
                result.AddRange(currentParent.Attributes);
                currentParent = currentParent.Parent;
            }

            return result;
        }

        private IEnumerable<PreconditionAttribute> BuildPreconditions(ModuleBuilder builder)
        {
            var preconditions = new List<PreconditionAttribute>();

            var parent = builder.Parent;

            while (parent != null)
            {
                preconditions.AddRange(parent.Preconditions);
                parent = parent.Parent;
            }

            return preconditions;
        } 
    }
}
