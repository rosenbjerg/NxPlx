﻿using System;
using NxPlx.Infrastructure.Events.Events;

namespace NxPlx.Application.Events.Authentication
{
    public class LoginAttemptCommand : IApplicationCommand<(string Token, DateTime Expiry, bool IsAdmin)>
    {
        public LoginAttemptCommand(string username, string password, string userAgent)
        {
            Username = username;
            Password = password;
            UserAgent = userAgent;
        }

        public string Username { get; }
        public string Password { get; }
        public string UserAgent { get; }
    }
}