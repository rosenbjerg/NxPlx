﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NxPlx.Application.Core;
using NxPlx.Application.Models;
using NxPlx.Models;
using NxPlx.Services.Database;

namespace NxPlx.Core.Services
{
    public class UserService
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IDtoMapper _dtoMapper;
        private readonly UserContextService _userContextService;

        public UserService(DatabaseContext context, ILogger<UserService> logger, IDtoMapper dtoMapper, UserContextService userContextService)
        {
            _context = context;
            _logger = logger;
            _dtoMapper = dtoMapper;
            _userContextService = userContextService;
        }

        public async Task<UserDto?> GetCurrentUser() => _dtoMapper.Map<User, UserDto>(await _userContextService.GetUser());
        
        public async Task UpdateUser(string? email)
        {
            var currentUser = await _userContextService.GetUser();
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);

            if (existingUser != null)
            {
                if (email != null)
                {
                    existingUser.Email = email;
                }

                await _context.SaveChangesAsync();
            }
        }
        public async Task RemoveUser(string username)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser == default) return;

            _context.Users.Remove(existingUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted user {Username}", existingUser.Username);
        }
        public async Task<bool> ChangeUserPassword(string oldPassword, string password1, string password2)
        {
            var currentUser = await _userContextService.GetUser();
            if (string.IsNullOrWhiteSpace(password1) || password1 != password2) return false;

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id);
            if (existingUser == null || !PasswordUtils.Verify(oldPassword, existingUser.PasswordHash)) return false;

            existingUser.PasswordHash = PasswordUtils.Hash(password1);
            existingUser.HasChangedPassword = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} changed password", existingUser.Username);
            return true;
        }
        public async Task<IEnumerable<UserDto>> ListUsers()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            return _dtoMapper.Map<User, UserDto>(users);
        }
        
        public async Task<UserDto?> GetUser(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return _dtoMapper.Map<User, UserDto>(user);
        }
        public async Task<UserDto> CreateUser(string username, string? email, bool isAdmin, List<int>? libraryIds, string password)
        {
            var user = new User
            {
                Username = username,
                Email = email,
                Admin = isAdmin,
                LibraryAccessIds = libraryIds ?? new List<int>(),
                PasswordHash = PasswordUtils.Hash(password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created user {Username}", user.Username);
            return _dtoMapper.Map<User, UserDto>(user)!;
        }
    }
}