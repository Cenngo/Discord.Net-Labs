using System;

namespace Discord.Interactions
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DefaultMemberPermissionAttribute : Attribute
    {
        public GuildPermission Permissions { get; }

        public DefaultMemberPermissionAttribute(GuildPermission permissions)
        {
            Permissions = permissions;
        }
    }
}
