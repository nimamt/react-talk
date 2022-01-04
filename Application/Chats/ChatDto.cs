﻿using System;
using System.Text.Json.Serialization;
using Application.Messages;

namespace Application.Chats
{
    public class ChatDto
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        //Private Chat
        public int PrivateChatId { get; set; }
        public string DisplayName { get; set; }
        public string Image { get; set; }
        public MessageDto LastMessage { get; set; }
        public bool LastMessageSeen { get; set; }
        public int NotSeenCount { get; set; }
        [JsonIgnore] public string ParticipantUsername { get; set; }
    }
}