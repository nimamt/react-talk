﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Chats.ChannelChats;
using Application.Core;
using AutoMapper;
using Domain;
using Domain.Direct;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Test.Mocks;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestApplicationLayer.Chats
{
    public class TestAddChannelMembers
    {
        [Fact]
        public async Task TestInvalidId()
        {
            var options = SqliteInMemory.CreateOptions<DataContext>();
            using (var context = new DataContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var users = new List<AppUser>();
                await Seed.SeedData(context, MockUserManager.Create(users).Object);

                var config = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfiles()); });
                var mapper = config.CreateMapper();

                var bob = await context.Users.FirstAsync(x => x.UserName == "bob");
                var channel = new ChannelChat { Members = new List<ChannelMembership>() };
                channel.Members.Add(new ChannelMembership {AppUser = bob, Channel = channel, MemberType = MemberType.Owner});
                var chat = new Chat { Type = ChatType.Channel, ChannelChat = channel };
                
                var dbChat = context.Add(chat);
                
                var request = new AddMembers.Command { Id = new Guid(),Members = new List<string>() {"tom"} };
                var userAccessor = MockUserAccessor.Create().Object;
                var handler = new AddMembers.Handler(context, mapper, userAccessor);

                //Act
                var result = await handler.Handle(request, new System.Threading.CancellationToken());

                var tomChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.AppUser.UserName == "tom");
                var bobChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AppUser.UserName == userAccessor.GetUsername());

                //Assert
                result.ShouldBeNull();
                tomChat.ShouldBeNull();
                bobChat.ShouldNotBeNull();
                Assert.NotEqual(tomChat.Chat.Id.ToString(), bobChat.Chat.Id.ToString());
            }
        }
        
        [Fact]
        public async Task TestPrivateChatBait()
        {
            var options = SqliteInMemory.CreateOptions<DataContext>();
            using (var context = new DataContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var users = new List<AppUser>();
                await Seed.SeedData(context, MockUserManager.Create(users).Object);

                var config = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfiles()); });
                var mapper = config.CreateMapper();

                var chat = new Chat { Type = ChatType.PrivateChat, PrivateChat = new PrivateChat() };
                
                var user = await context.Users.FirstOrDefaultAsync(x => x.UserName == "bob");
                var tom = await context.Users.FirstOrDefaultAsync(x => x.UserName == "tom");
                
                var userChat = new UserChat { Chat = chat, AppUser = user };
                var userChat1 = new UserChat { Chat = chat, AppUser = tom };
                
                var realEntry = context.UserChats.Add(userChat);
                context.UserChats.Add(userChat1);
                await context.SaveChangesAsync();

                var id = realEntry.Entity.Chat.Id;                
                
                var request = new AddMembers.Command { Id = id,Members = new List<string>() {"tom"} };
                var userAccessor = MockUserAccessor.Create().Object;
                var handler = new AddMembers.Handler(context, mapper, userAccessor);

                //Act
                var result = await handler.Handle(request, new System.Threading.CancellationToken());

                var tomChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.AppUser.UserName == "tom");
                var bobChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AppUser.UserName == userAccessor.GetUsername());

                //Assert
                result.ShouldBeNull();
                tomChat.ShouldBeNull();
                bobChat.ShouldNotBeNull();
                Assert.NotEqual(tomChat.Chat.Id.ToString(), bobChat.Chat.Id.ToString());
            }
        }
        
        [Fact]
        public async Task TestFlawless()
        {
            var options = SqliteInMemory.CreateOptions<DataContext>();
            using (var context = new DataContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                var users = new List<AppUser>();
                await Seed.SeedData(context, MockUserManager.Create(users).Object);

                var config = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfiles()); });
                var mapper = config.CreateMapper();

                var bob = await context.Users.FirstAsync(x => x.UserName == "bob");
                var channel = new ChannelChat { Members = new List<ChannelMembership>() };
                channel.Members.Add(new ChannelMembership {AppUser = bob, Channel = channel, MemberType = MemberType.Owner});
                var chat = new Chat { Type = ChatType.Channel, ChannelChat = channel };
                
                var dbChat = context.Add(chat);
                
                
                var request = new AddMembers.Command { Id=dbChat.Entity.Id, Members = new List<string>() {"tom"} };
                var userAccessor = MockUserAccessor.Create().Object;
                var handler = new AddMembers.Handler(context, mapper, userAccessor);

                //Act
                var result = await handler.Handle(request, new System.Threading.CancellationToken());

                var tomChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking().FirstOrDefaultAsync(x => x.AppUser.UserName == "tom");
                var bobChat = await context.UserChats
                    .Include(x => x.Chat)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.AppUser.UserName == userAccessor.GetUsername());

                //Assert
                result.ShouldNotBeNull();
                tomChat.ShouldNotBeNull();
                bobChat.ShouldNotBeNull();
                Assert.Equal(tomChat.Chat.Id.ToString(), bobChat.Chat.Id.ToString());
            }
        }
    }
}