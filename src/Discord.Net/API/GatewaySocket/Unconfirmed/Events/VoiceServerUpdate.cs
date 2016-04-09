﻿using Newtonsoft.Json;

namespace Discord.API.GatewaySocket
{
    public class VoiceServerUpdateEvent
    {
        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }
        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}