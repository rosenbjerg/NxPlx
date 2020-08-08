﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using NxPlx.Application.Core;
using NxPlx.Core.Services;
using NxPlx.Models;
using NxPlx.Services.Database;

namespace NxPlx.Services.Index
{
    public class ImageProcessor
    {
        private readonly DatabaseContext _context;
        private readonly IDetailsApi _detailsApi;
        private readonly TempFileService _tempFileService;
        private readonly ImageCreator _imageCreator;

        public ImageProcessor(DatabaseContext context, IDetailsApi detailsApi, TempFileService tempFileService, ImageCreator imageCreator)
        {
            _context = context;
            _detailsApi = detailsApi;
            _tempFileService = tempFileService;
            _imageCreator = imageCreator;
        }
        
        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessFilmDetails(long filmDetailsId)
        {
            var filmDetails = await _context.FilmDetails.FindAsync(filmDetailsId);

            var poster = await ProcessPoster(filmDetails);
            var backdrop = await ProcessBackdrop(filmDetails);
            
            if (poster || backdrop)
                await _context.SaveChangesAsync();
        }

        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessMovieCollection(long movieCollectionId)
        {
            var movieCollection = await _context.MovieCollection.FindAsync(movieCollectionId);

            var poster = await ProcessPoster(movieCollection);
            var backdrop = await ProcessBackdrop(movieCollection);
            
            if (poster || backdrop)
                await _context.SaveChangesAsync();
        }

        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessNetworks(ICollection<int> networkIds)
        {
            var networks = await _context.Network.Where(n => networkIds.Contains(n.Id)).ToListAsync();

            var updated = false;
            foreach (var network in networks)
            {
                updated = await ProcessLogo(network) || updated;
            }

            if (updated)
                await _context.SaveChangesAsync();
        }

        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessProductionCompanies(ICollection<int> productionCompanyIds)
        {
            var productionCompanies = await _context.ProductionCompany.Where(n => productionCompanyIds.Contains(n.Id)).ToListAsync();

            var updated = false;
            foreach (var productionCompany in productionCompanies)
            {
                updated = await ProcessLogo(productionCompany) || updated;
            }

            if (updated)
                await _context.SaveChangesAsync();
        }

        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessSeries(int seriesDetailsId)
        {
            var seriesDetails = await _context.SeriesDetails.Include(sd => sd.Seasons)
                .FirstOrDefaultAsync(sd => sd.Id == seriesDetailsId);

            await ProcessPoster(seriesDetails);
            await ProcessBackdrop(seriesDetails);
            
            foreach (var season in seriesDetails.Seasons)
            {
                await ProcessPoster(season);
                BackgroundJob.Enqueue<ImageProcessor>(service => service.GenerateEpisodeStills(seriesDetailsId, season.Id));
            }
            await _context.SaveChangesAsync();
        }

        [Queue(JobQueueNames.ImageProcessing)]
        [AutomaticRetry(Attempts = 0)]
        public async Task GenerateEpisodeStills(int seriesDetailsId, int seasonDetailsId)
        {
            var episodes = await _context.EpisodeDetails.Where(e => e.SeasonDetailsId == seasonDetailsId && e.StillBlurHash == null).ToListAsync();
            if (!episodes.Any()) return;

            var seasonNumber = episodes.First().SeasonNumber;
            var episodeNumbers = episodes.Select(e => e.EpisodeNumber).ToList();
            var paths = await _context.EpisodeFiles
                .Where(e => e.SeriesDetailsId == seriesDetailsId && e.SeasonNumber == seasonNumber && episodeNumbers.Contains(e.EpisodeNumber))
                .ToDictionaryAsync(e => e.EpisodeNumber, e => e.Path);
            
            foreach (var episodeDetails in episodes)
            {
                if (!paths.TryGetValue(episodeDetails.EpisodeNumber, out var path))
                    continue;
                
                var snapshotTempPath = await _imageCreator.CreateSnapshot(path, 0.2);
                await _imageCreator.SetStill(episodeDetails, snapshotTempPath, $"{Guid.NewGuid()}.jpg");
            }

            await _context.SaveChangesAsync();
        }

        private async Task<bool> ProcessPoster(IPosterImageOwner imageOwner)
        {
            if (string.IsNullOrEmpty(imageOwner.PosterPath) || !string.IsNullOrEmpty(imageOwner.PosterBlurHash)) return false;
            
            var tempFile = _tempFileService.GetFilename("download", ".jpg");
            await _detailsApi.DownloadImage(342, imageOwner.PosterPath, tempFile);
            await _imageCreator.SetPoster(imageOwner, tempFile, imageOwner.PosterPath);
            return true;
        }

        private async Task<bool> ProcessBackdrop(IBackdropImageOwner imageOwner)
        {
            if (string.IsNullOrEmpty(imageOwner.BackdropPath) || !string.IsNullOrEmpty(imageOwner.BackdropBlurHash)) return false;
            
            var tempFile = _tempFileService.GetFilename("download", ".jpg");
            await _detailsApi.DownloadImage(1280, imageOwner.BackdropPath, tempFile);
            await _imageCreator.SetBackdrop(imageOwner, tempFile, imageOwner.BackdropPath);
            await _context.SaveChangesAsync();
            return true;
        }
        private async Task<bool> ProcessLogo(ILogoImageOwner imageOwner)
        {
            if (string.IsNullOrEmpty(imageOwner.LogoPath) || !string.IsNullOrEmpty(imageOwner.LogoBlurHash)) return false;
            
            var tempFile = _tempFileService.GetFilename("download", Path.GetExtension(imageOwner.LogoPath));
            await _detailsApi.DownloadImage(154, imageOwner.LogoPath, tempFile);
            await _imageCreator.SetLogo(imageOwner, tempFile, imageOwner.LogoPath);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}