﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Core;
using Application.Interfaces;
using AutoMapper;
using Domain.Direct;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Messages
{
    public class Create
    {
        public class Command : IRequest<Result<MessageDto>>
        {
            public Guid ChatId { get; set; }
            public string Body { get; set; }
        }

        public class Handler : IRequestHandler<Command, Result<MessageDto>>
        {
            private readonly DataContext _context;
            private readonly IMapper _mapper;
            private readonly IUserAccessor _userAccessor;

            public Handler(DataContext context, IMapper mapper, IUserAccessor userAccessor)
            {
                _context = context;
                _mapper = mapper;
                _userAccessor = userAccessor;
            }
            
            public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
            {
                var userChat = await _context
                    .UserChats
                    .Include(x => x.AppUser)
                    .Include(x => x.Chat)
                    .Include(x => x.Chat.PrivateChat)
                    .SingleOrDefaultAsync(x => x.ChatId == request.ChatId && 
                        x.AppUser.UserName == _userAccessor.GetUsername(), cancellationToken);

                if (userChat == null)
                    return null;

                var message = new Message
                {
                    Type = MessageType.Text,
                    Body = request.Body,
                    Sender = userChat.AppUser
                };
                
                userChat.Chat.PrivateChat.Messages.Add(message);

                var result = await _context.SaveChangesAsync(cancellationToken);

                if (result > 0)
                    return Result<MessageDto>.Success(_mapper.Map<MessageDto>(message));
                else
                    return Result<MessageDto>.Failure("Failed to add the message to the private chat");
            }
        }
    }
}