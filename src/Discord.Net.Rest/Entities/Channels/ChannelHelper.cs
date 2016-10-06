﻿using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    internal static class ChannelHelper
    {
        //General
        public static async Task DeleteAsync(IChannel channel, BaseDiscordClient client, 
            RequestOptions options)
        { 
            await client.ApiClient.DeleteChannelAsync(channel.Id, options).ConfigureAwait(false);
        }
        public static async Task ModifyAsync(IGuildChannel channel, BaseDiscordClient client, 
            Action<ModifyGuildChannelParams> func, 
            RequestOptions options)
        {
            var args = new ModifyGuildChannelParams();
            func(args);
            await client.ApiClient.ModifyGuildChannelAsync(channel.Id, args, options);
        }
        public static async Task ModifyAsync(ITextChannel channel, BaseDiscordClient client, 
            Action<ModifyTextChannelParams> func, 
            RequestOptions options)
        {
            var args = new ModifyTextChannelParams();
            func(args);
            await client.ApiClient.ModifyGuildChannelAsync(channel.Id, args, options);
        }
        public static async Task ModifyAsync(IVoiceChannel channel, BaseDiscordClient client, 
            Action<ModifyVoiceChannelParams> func, 
            RequestOptions options)
        {
            var args = new ModifyVoiceChannelParams();
            func(args);
            await client.ApiClient.ModifyGuildChannelAsync(channel.Id, args, options);
        }

        //Invites
        public static async Task<IReadOnlyCollection<RestInviteMetadata>> GetInvitesAsync(IChannel channel, BaseDiscordClient client,
            RequestOptions options)
        {
            var models = await client.ApiClient.GetChannelInvitesAsync(channel.Id, options);
            return models.Select(x => RestInviteMetadata.Create(client, x)).ToImmutableArray();
        }
        public static async Task<RestInviteMetadata> CreateInviteAsync(IChannel channel, BaseDiscordClient client,
            int? maxAge, int? maxUses, bool isTemporary, RequestOptions options)
        {
            var args = new CreateChannelInviteParams { IsTemporary = isTemporary };
            if (maxAge.HasValue)
                args.MaxAge = maxAge.Value;
            if (maxUses.HasValue)
                args.MaxUses = maxUses.Value;
            var model = await client.ApiClient.CreateChannelInviteAsync(channel.Id, args, options);
            return RestInviteMetadata.Create(client, model);
        }

        //Messages
        public static async Task<RestMessage> GetMessageAsync(IChannel channel, BaseDiscordClient client, 
            ulong id, RequestOptions options)
        {
            var model = await client.ApiClient.GetChannelMessageAsync(channel.Id, id, options).ConfigureAwait(false);
            return RestMessage.Create(client, model);
        }
        public static IAsyncEnumerable<IReadOnlyCollection<RestMessage>> GetMessagesAsync(IChannel channel, BaseDiscordClient client, 
            ulong? fromMessageId, Direction dir, int limit, RequestOptions options)
        {
            //TODO: Test this with Around direction
            return new PagedAsyncEnumerable<RestMessage>(
                DiscordConfig.MaxMessagesPerBatch,
                async (info, ct) =>
                {
                    var args = new GetChannelMessagesParams
                    {
                        RelativeDirection = dir,
                        Limit = info.PageSize
                    };
                    if (info.Position != null)
                        args.RelativeMessageId = info.Position.Value;
                    var models = await client.ApiClient.GetChannelMessagesAsync(channel.Id, args, options);
                    return models.Select(x => RestMessage.Create(client, x)).ToImmutableArray(); ;
                },
                nextPage: (info, lastPage) =>
                {
                    if (dir == Direction.Before)
                        info.Position = lastPage.Min(x => x.Id);
                    else
                        info.Position = lastPage.Max(x => x.Id);
                    if (lastPage.Count != DiscordConfig.MaxMessagesPerBatch)
                        info.Remaining = 0;
                },
                start: fromMessageId,
                count: limit
            );
        }
        public static async Task<IReadOnlyCollection<RestMessage>> GetPinnedMessagesAsync(IChannel channel, BaseDiscordClient client, 
            RequestOptions options)
        {
            var models = await client.ApiClient.GetPinsAsync(channel.Id, options).ConfigureAwait(false);
            return models.Select(x => RestMessage.Create(client, x)).ToImmutableArray();
        }

        public static async Task<RestUserMessage> SendMessageAsync(IChannel channel, BaseDiscordClient client,
            string text, bool isTTS, RequestOptions options)
        {
            var args = new CreateMessageParams(text) { IsTTS = isTTS };
            var model = await client.ApiClient.CreateMessageAsync(channel.Id, args, options).ConfigureAwait(false);
            return RestUserMessage.Create(client, model);
        }

        public static Task<RestUserMessage> SendFileAsync(IChannel channel, BaseDiscordClient client,
            string filePath, string text, bool isTTS, RequestOptions options)
        {
            string filename = Path.GetFileName(filePath);
            using (var file = File.OpenRead(filePath))
                return SendFileAsync(channel, client, file, filename, text, isTTS, options);
        }
        public static async Task<RestUserMessage> SendFileAsync(IChannel channel, BaseDiscordClient client,
            Stream stream, string filename, string text, bool isTTS, RequestOptions options)
        {
            var args = new UploadFileParams(stream) { Filename = filename, Content = text, IsTTS = isTTS };
            var model = await client.ApiClient.UploadFileAsync(channel.Id, args, options).ConfigureAwait(false);
            return RestUserMessage.Create(client, model);
        }

        public static async Task DeleteMessagesAsync(IChannel channel, BaseDiscordClient client, 
            IEnumerable<IMessage> messages, RequestOptions options)
        {
            var args = new DeleteMessagesParams(messages.Select(x => x.Id).ToArray());
            await client.ApiClient.DeleteMessagesAsync(channel.Id, args, options).ConfigureAwait(false);
        }

        //Permission Overwrites
        public static async Task AddPermissionOverwriteAsync(IGuildChannel channel, BaseDiscordClient client,
            IUser user, OverwritePermissions perms, RequestOptions options)
        {
            var args = new ModifyChannelPermissionsParams("member", perms.AllowValue, perms.DenyValue);
            await client.ApiClient.ModifyChannelPermissionsAsync(channel.Id, user.Id, args, options).ConfigureAwait(false);
        }
        public static async Task AddPermissionOverwriteAsync(IGuildChannel channel, BaseDiscordClient client,
            IRole role, OverwritePermissions perms, RequestOptions options)
        {
            var args = new ModifyChannelPermissionsParams("role", perms.AllowValue, perms.DenyValue);
            await client.ApiClient.ModifyChannelPermissionsAsync(channel.Id, role.Id, args, options).ConfigureAwait(false);
        }
        public static async Task RemovePermissionOverwriteAsync(IGuildChannel channel, BaseDiscordClient client,
            IUser user, RequestOptions options)
        {
            await client.ApiClient.DeleteChannelPermissionAsync(channel.Id, user.Id, options).ConfigureAwait(false);
        }
        public static async Task RemovePermissionOverwriteAsync(IGuildChannel channel, BaseDiscordClient client,
            IRole role, RequestOptions options)
        {
            await client.ApiClient.DeleteChannelPermissionAsync(channel.Id, role.Id, options).ConfigureAwait(false);
        }

        //Users
        public static async Task<RestGuildUser> GetUserAsync(IGuildChannel channel, IGuild guild, BaseDiscordClient client,
            ulong id, RequestOptions options)
        {
            var model = await client.ApiClient.GetGuildMemberAsync(channel.GuildId, id, options);
            if (model == null)
                return null;
            var user = RestGuildUser.Create(client, guild, model);
            if (!user.GetPermissions(channel).ReadMessages)
                return null;

            return user;
        }
        public static IAsyncEnumerable<IReadOnlyCollection<RestGuildUser>> GetUsersAsync(IGuildChannel channel, IGuild guild, BaseDiscordClient client,
            ulong? fromUserId, int? limit, RequestOptions options)
        {
            return new PagedAsyncEnumerable<RestGuildUser>(
                DiscordConfig.MaxUsersPerBatch,
                async (info, ct) =>
                {
                    var args = new GetGuildMembersParams
                    {
                        Limit = info.PageSize
                    };
                    if (info.Position != null)
                        args.AfterUserId = info.Position.Value;
                    var models = await guild.Discord.ApiClient.GetGuildMembersAsync(guild.Id, args, options);
                    return models
                        .Select(x => RestGuildUser.Create(client, guild, x))
                        .Where(x => x.GetPermissions(channel).ReadMessages)
                        .ToImmutableArray();
                },
                nextPage: (info, lastPage) =>
                {
                    info.Position = lastPage.Max(x => x.Id);
                    if (lastPage.Count != DiscordConfig.MaxMessagesPerBatch)
                        info.Remaining = 0;
                },
                start: fromUserId,
                count: limit
            );
        }

        //Typing
        public static async Task TriggerTypingAsync(IMessageChannel channel, BaseDiscordClient client,
            RequestOptions options = null)
        {
            await client.ApiClient.TriggerTypingIndicatorAsync(channel.Id, options);
        }
        public static IDisposable EnterTypingState(IMessageChannel channel, BaseDiscordClient client, 
            RequestOptions options)
            => new TypingNotifier(client, channel, options);
    }
}
