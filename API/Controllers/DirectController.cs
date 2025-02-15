﻿using System;
using System.Threading.Tasks;
using API.DTOs;
using API.SignalR;
using Application;
using Application.Chats;
using Application.Chats.ChannelChats;
using Application.Chats.UserChats;
using Application.Core;
using Application.Interfaces;
using Application.Typing;
using Domain.Direct;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Create = Application.Messages.Create;
using Details = Application.Chats.PrivateChats.Details;


namespace API.Controllers
{
    public class DirectController : BaseApiController
    {
        private readonly IHubContext<DirectHub> _hubContext;
        private readonly IUserAccessor _accessor;

        public DirectController(IHubContext<DirectHub> hubContext, IUserAccessor accessor)
        {
            _hubContext = hubContext;
            _accessor = accessor;
        }

        [HttpGet()]
        public async Task<IActionResult> GetChats([FromQuery] PagingParams param)
        {
            return HandlePagedResult(await Mediator.Send(new Application.Chats.List.Query { Params = param }));
        }

        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages([FromQuery] PagingParams param, [FromQuery] Guid chatId)
        {
            return HandlePagedResult(await Mediator.Send(new ListMessages.Query {Params = param, ChatId = chatId}));
        }

        [HttpPost()]
        public async Task<IActionResult> CreatePrivateChat(AddPrivateChat.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }
        
        [HttpPost("savedMessagesChat")]
        public async Task<IActionResult> CreateSavedMessagesChat(AddSavedChat.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpGet("privateChatDetails/{chatId}")]
        public async Task<IActionResult> GetPrivateChatDetails(Guid chatId)
        {
            var result = await Mediator.Send(new Details.Query {ChatId = chatId});
            
            return HandleResult(result);
        }

        [HttpPost("messages")]
        public async Task<IActionResult> CreateMessage(Create.Command command)
        {
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                var users = await Mediator
                    .Send(new Application.Chats.UserChats.List.Query { ChatId = command.ChatId });
                
                foreach (var u in users.Value)
                {
                    var notSeenCount = await Mediator
                        .Send(new NotSeenCount.Query { ChatId = command.ChatId, TargetUserId = u });
                    await _hubContext.Clients.User(u).SendAsync("ReceiveNewMessage", new MessageNotifDto
                    {
                        Message = result.Value,
                        ChatId = command.ChatId,
                        NotSeenCount = notSeenCount.Value
                    });
                }
            }
            return HandleResult(result);
        }
        
        [HttpPost("photos")]
        public async Task<IActionResult> CreatePhoto([FromForm] Application.Messages.Images.Create.Command command)
        {
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                var users = await Mediator
                    .Send(new Application.Chats.UserChats.List.Query { ChatId = command.ChatId });
                
                foreach (var u in users.Value)
                {
                    await _hubContext.Clients.User(u).SendAsync("ReceiveNewMessage", new MessageNotifDto
                    {
                        Message = result.Value,
                        ChatId = command.ChatId   
                    });
                }
            }
            return HandleResult(result);
        }
        
        [HttpPost("videos")]
        public async Task<IActionResult> CreateVideo([FromForm] Application.Messages.Videos.Create.Command command)
        {
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                var users = await Mediator
                    .Send(new Application.Chats.UserChats.List.Query { ChatId = command.ChatId });
                
                foreach (var u in users.Value)
                {
                    await _hubContext.Clients.User(u).SendAsync("ReceiveNewMessage", new MessageNotifDto
                    {
                        Message = result.Value,
                        ChatId = command.ChatId   
                    });
                }
            }
            return HandleResult(result);
        }
        
        [HttpPost("voices")]
        public async Task<IActionResult> CreateMusic([FromForm] Application.Messages.Voice.Create.Command command)
        {
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                var users = await Mediator
                    .Send(new Application.Chats.UserChats.List.Query { ChatId = command.ChatId });
                
                foreach (var u in users.Value)
                {
                    await _hubContext.Clients.User(u).SendAsync("ReceiveNewMessage", new MessageNotifDto
                    {
                        Message = result.Value,
                        ChatId = command.ChatId   
                    });
                }
            }
            return HandleResult(result);
        }
        
        [HttpPost("updateSeen")]
        public async Task<IActionResult> UpdateSeen(UpdateSeen.Command command)
        {
            var result = await Mediator.Send(command);
            
            if (result.IsSuccess)
            {
                var users = await Mediator
                    .Send(new Application.Chats.UserChats.List.Query { ChatId = command.ChatId });
                
                foreach (var u in users.Value)
                {
                    await _hubContext.Clients.User(u).SendAsync("ReceiveNewSeen", 
                        new UpdatedSeenDto {Username = _accessor.GetUsername(), 
                            ChatId = command.ChatId,
                            LastSeen = command.NewLastSeen
                        });
                }
            }
            return HandleResult(result);
        }
        
        [HttpPost("addMember")]
        public async Task<IActionResult> AddMembers(AddMembers.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }
        
        [HttpPost("removeMember")]
        public async Task<IActionResult> RemoveMember(RemoveMember.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpPost("addPin")]
        public async Task<IActionResult> AddPin(AddPin.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpPost("removePin")]
        public async Task<IActionResult> RemovePin(RemovePin.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }

        [HttpPost("forward")]
        public async Task<IActionResult> Forward(ForwardMessages.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }
        
        [HttpPut("deleteMessage")]
        public async Task<IActionResult> DeleteMessage(DeleteMessage.Command command)
        {
            return HandleResult(await Mediator.Send(command));
        }
        
        [HttpPut("typing")]
        public async Task<IActionResult> StartTyping(GetTypeInfo.Query query)
        {
            var result = await Mediator.Send(query);

            foreach (var user in result.Value.Members)
            {
                await _hubContext.Clients.User(user).SendAsync("StartedTyping", 
                    new TypingDto { 
                        Username = _accessor.GetUsername(),
                        DisplayName = result.Value.DisplayName,
                        ChatId = query.ChatId
                    });
            }
            
            return HandleResult(Result<Unit>.Success(Unit.Value));
        }
        
        [HttpPut("stopTyping")]
        public async Task<IActionResult> StopTyping(GetTypeInfo.Query query)
        {
            var result = await Mediator.Send(query);

            foreach (var user in result.Value.Members)
            {
                await _hubContext.Clients.User(user).SendAsync("StoppedTyping", 
                    new TypingDto { 
                        Username = _accessor.GetUsername(),
                        DisplayName = result.Value.DisplayName,
                        ChatId = query.ChatId
                    });
            }
            
            return HandleResult(Result<Unit>.Success(Unit.Value));
        }
        
        [HttpGet("chat/{chatId}")]
        public async Task<IActionResult> GetChat(Guid chatId)
        {
            var result = await Mediator.Send(new Get.Query {ChatId = chatId});
            
            return HandleResult(result);
        }
    }
}