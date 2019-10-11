﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using NxPlx.Abstractions;
using NxPlx.Configuration;
using NxPlx.Infrastructure.Broadcasting;
using NxPlx.Infrastructure.IoC;
using NxPlx.Infrastructure.Logging;
using NxPlx.Infrastructure.Session;
using NxPlx.Integrations.TMDb;
using NxPlx.Models;
using NxPlx.Services.Caching;
using NxPlx.Services.Database;
using NxPlx.Services.Index;
using NxPlx.WebApi.Routers;
using Red;
using Red.CookieSessions;
using Red.CookieSessions.EFCore;

namespace NxPlx.WebApi
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            var cfg = ConfigurationService.Current;

            {
                var registration = new RegistrationContainer();
                registration.Register<ICachingService, RedisCachingService>();
                registration.Register<ILoggingService, NLoggingService>();
                registration.Register<IDatabaseMapper, DatabaseMapper>();
                registration.Register<IDetailsMapper, TMDbMapper>(false);
                registration.Register<IDetailsApi, TMDbApi>(false);
                registration.Register<Indexer>(false);
                registration.Register<MediaContext>(false);
                registration.Register<UserContext>(false);

                var broadcaster = new WebSocketBroadcaster();
                registration.Register<IBroadcaster, WebSocketBroadcaster>(broadcaster);
                registration.Register<IBroadcaster<string, WebSocketDialog>, WebSocketBroadcaster>(broadcaster);
            }
            
            var container = ResolveContainer.Default();
            var logger = container.Resolve<ILoggingService>();
            logger.Info("NxPlx.Infrastructure.WebApi starting...");

            var server = new RedHttpServer(cfg.HttpPort);
            server.Use(new CookieSessions<UserSession>(TimeSpan.FromDays(14))
            {
                Secure = cfg.Production,
                Path = "/",
                Store = new EntityFrameworkSessionStore<UserSession>(container.Resolve<UserContext>)
            });
            server.OnHandlerException += (sender, eventArgs) =>
            {
                Console.WriteLine(eventArgs);
            }; 
            
            var databaseContextManager = new DatabaseContextManager();
            server.RespondWithExceptionDetails = true;
            server.ConfigureServices = databaseContextManager.Register;
            await databaseContextManager.Initialize();
            
            AuthenticationRoutes.Register(server.CreateRouter("/api/authentication"));
            UserRoutes.Register(server.CreateRouter("/api/user"));
            LibraryRoutes.Register(server.CreateRouter("/api/library"));
            OverviewRoutes.Register(server.CreateRouter("/api/overview"));
            EpisodeRoutes.Register(server.CreateRouter("/api/series"));
            FilmRoutes.Register(server.CreateRouter("/api/film"));
            
            IndexingRoutes.Register(server.CreateRouter("/api/indexing"));
            BroadcastRoutes.Register(server.CreateRouter("/api/broadcast"));
            SubtitleRoutes.Register(server.CreateRouter("/api/subtitle"));
            ProgressRoutes.Register(server.CreateRouter("/api/progress"));
            ImageRoutes.Register(server.CreateRouter("/api/image"));
            
            await server.RunAsync();
        }
    }
}