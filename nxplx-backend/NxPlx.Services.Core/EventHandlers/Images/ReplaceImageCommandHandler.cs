﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NxPlx.Application.Core;
using NxPlx.Application.Models;
using NxPlx.Application.Models.Events;
using NxPlx.Application.Models.Events.Images;
using NxPlx.Infrastructure.Database;
using NxPlx.Models;

namespace NxPlx.Core.Services.EventHandlers.Images
{
    public class ReplaceImageCommandHandler : IEventHandler<ReplaceImageCommand, bool>
    {
        private readonly DatabaseContext _context;
        private readonly TempFileService _tempFileService;
        private readonly ICacheClearer _cacheClearer;
        private readonly IEventDispatcher _dispatcher;

        public ReplaceImageCommandHandler(DatabaseContext context, TempFileService tempFileService, ICacheClearer cacheClearer, IEventDispatcher dispatcher)
        {
            _context = context;
            _tempFileService = tempFileService;
            _cacheClearer = cacheClearer;
            _dispatcher = dispatcher;
        }

        public async Task<bool> Handle(ReplaceImageCommand command, CancellationToken cancellationToken = default)
        {
            var task = command.DetailsType switch
            {
                DetailsType.Series => SetSeriesImage(command.DetailsId, command.ImageType, command.ImageExtension, command.ImageStream),
                DetailsType.Season => SetSeasonImage(command.DetailsId, command.ImageType, command.ImageExtension, command.ImageStream),
                DetailsType.Film => SetFilmImage(command.DetailsId, command.ImageType, command.ImageExtension, command.ImageStream),
                DetailsType.Collection => SetCollectionImage(command.DetailsId, command.ImageType, command.ImageExtension, command.ImageStream),
                _ => throw new ArgumentOutOfRangeException()
            };
            var success = await task;
            if (!success) return false;
            await _context.SaveChangesAsync(CancellationToken.None);
            await _cacheClearer.Clear("OVERVIEW");
            return true;
        }

        private async Task<bool> SetSeriesImage(int detailsId, ImageType imageType, string imageExtension, Stream imageStream)
        {
            var series = await _context.SeriesDetails.FirstOrDefaultAsync(sd => sd.Id == detailsId);
            if (series == null) return false;

            var tempFile = await SaveTempImage(imageExtension, imageStream);
            var task = imageType switch
            {
                ImageType.Poster => _dispatcher.Dispatch(new SetImageCommand<IPosterImageOwner>(series, tempFile, $"{Guid.NewGuid()}.jpg")),
                ImageType.Backdrop => _dispatcher.Dispatch(new SetImageCommand<IBackdropImageOwner>(series, tempFile, $"{Guid.NewGuid()}.jpg")),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            await task;
            return true;
        }

        private async Task<bool> SetSeasonImage(int detailsId, ImageType imageType, string imageExtension, Stream imageStream)
        {
            var season = await _context.SeasonDetails.FirstOrDefaultAsync(sd => sd.Id == detailsId);
            if (season == null) return false;

            var tempFile = await SaveTempImage(imageExtension, imageStream);
            var task = imageType switch
            {
                ImageType.Poster => _dispatcher.Dispatch(new SetImageCommand<IPosterImageOwner>(season, tempFile, $"{Guid.NewGuid()}.jpg")),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            await task;
            return true;
        }
        private async Task<bool> SetFilmImage(int detailsId, ImageType imageType, string imageExtension, Stream imageStream)
        {
            var film = await _context.FilmDetails.FirstOrDefaultAsync(fd => fd.Id == detailsId);
            if (film == null) return false;

            var tempFile = await SaveTempImage(imageExtension, imageStream);
            var task = imageType switch
            {
                ImageType.Poster => _dispatcher.Dispatch(new SetImageCommand<IPosterImageOwner>(film, tempFile, $"{Guid.NewGuid()}.jpg")),
                ImageType.Backdrop => _dispatcher.Dispatch(new SetImageCommand<IBackdropImageOwner>(film, tempFile, $"{Guid.NewGuid()}.jpg")),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            await task;
            return true;
        }
        private async Task<bool> SetCollectionImage(int detailsId, ImageType imageType, string imageExtension, Stream imageStream)
        {
            var collection = await _context.MovieCollection.FirstOrDefaultAsync(mc => mc.Id == detailsId);
            if (collection == null) return false;

            var tempFile = await SaveTempImage(imageExtension, imageStream);
            var task = imageType switch
            {
                ImageType.Poster => _dispatcher.Dispatch(new SetImageCommand<IPosterImageOwner>(collection, tempFile, $"{Guid.NewGuid()}.jpg")),
                ImageType.Backdrop => _dispatcher.Dispatch(new SetImageCommand<IBackdropImageOwner>(collection, tempFile, $"{Guid.NewGuid()}.jpg")),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            await task;
            return true;
        }

        private async Task<string> SaveTempImage(string imageExtension, Stream imageStream)
        {
            var tempFile = _tempFileService.GetFilename("image_upload", imageExtension);
            await using (var outputStream = System.IO.File.OpenWrite(tempFile))
                await imageStream.CopyToAsync(outputStream);
            return tempFile;
        }
    }
}