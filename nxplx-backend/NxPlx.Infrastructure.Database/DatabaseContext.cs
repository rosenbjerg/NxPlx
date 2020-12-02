using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NxPlx.Application.Core;
using NxPlx.Models;
using NxPlx.Models.Database;
using NxPlx.Models.Details;
using NxPlx.Models.Details.Film;
using NxPlx.Models.Details.Series;
using NxPlx.Models.File;

namespace NxPlx.Infrastructure.Database
{
    public class DatabaseContext : DbContext
    {
        private readonly IOperationContext _operationContext;
        public DatabaseContext(DbContextOptions<DatabaseContext> options, IOperationContext IOperationContext)
            : base(options)
        {
            _operationContext = IOperationContext;
        }
        // public DatabaseContext()
        // {
        //     _operationContext = new IOperationContext();
        // }
        //
        // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        // {
        //     base.OnConfiguring(optionsBuilder.UseNpgsql("Host=localhost;Database=nxplx_db;Username=postgres;Password=dev"));
        // }

        public DbSet<FilmFile> FilmFiles { get; set; } = null!;
        public DbSet<DbFilmDetails> FilmDetails { get; set; } = null!;
        public DbSet<EpisodeFile> EpisodeFiles { get; set; } = null!;
        public DbSet<DbSeriesDetails> SeriesDetails { get; set; } = null!;
        public DbSet<Library> Libraries { get; set; } = null!;
        public DbSet<SubtitleFile> SubtitleFiles { get; set; } = null!;
        public DbSet<SubtitlePreference> SubtitlePreferences { get; set; } = null!;
        public DbSet<WatchingProgress> WatchingProgresses { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Genre> Genre { get; set; } = null!;
        public DbSet<MovieCollection> MovieCollection { get; set; } = null!;
        public DbSet<Network> Network { get; set; } = null!;
        public DbSet<ProductionCompany> ProductionCompany { get; set; } = null!;
        public DbSet<EpisodeDetails> EpisodeDetails { get; set; } = null!;
        public DbSet<SeasonDetails> SeasonDetails { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            ConfigureEpisodeFileEntity(modelBuilder);

            ConfigureFilmFileEntity(modelBuilder);


            modelBuilder.Entity<DbSeriesDetails>().HasMany(s => s.Seasons).WithOne().OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DbSeriesDetails>().HasMany(s => s.ProductionCompanies).WithMany(pc => pc.ProducedSeries);
            modelBuilder.Entity<DbSeriesDetails>().HasMany(s => s.Genres).WithMany(g => g.SeriesInGenre);
            modelBuilder.Entity<DbSeriesDetails>().HasMany(s => s.Networks).WithMany(g => g.AiredSeries);
            modelBuilder.Entity<DbSeriesDetails>().HasMany(s => s.CreatedBy).WithMany(g => g.CreatedSeries);
            
            modelBuilder.Entity<SeasonDetails>().HasMany(s => s.Episodes).WithOne().OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DbFilmDetails>().HasOne(fd => fd.BelongsInCollection).WithMany(mc => mc.Movies).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DbFilmDetails>().HasMany(fd => fd.Genres).WithMany(g => g.FilmInGenre);
            modelBuilder.Entity<DbFilmDetails>().HasMany(fd => fd.ProductionCompanies).WithMany(g => g.ProducedFilm);
            modelBuilder.Entity<DbFilmDetails>().HasMany(fd => fd.ProductionCountries).WithMany(g => g.FilmedOnLocation);
            modelBuilder.Entity<DbFilmDetails>().HasMany(fd => fd.SpokenLanguages).WithMany(g => g.SpokenInFilm);

            modelBuilder.Entity<ProductionCountry>().HasKey(pc => pc.Iso3166_1);
            modelBuilder.Entity<SpokenLanguage>().HasKey(sl => sl.Iso639_1);

            modelBuilder.Entity<SubtitlePreference>(builder =>
            {
                builder.HasKey(sp => new { sp.UserId, sp.FileId, sp.MediaType });
                builder.HasIndex(sp => sp.UserId);
                builder.HasIndex(sp => sp.FileId);
                builder.HasIndex(sp => sp.MediaType);
            });
            
            modelBuilder.Entity<WatchingProgress>(builder =>
            {
                builder.HasKey(wp => new { wp.UserId, wp.FileId, wp.MediaType });
                builder.HasIndex(wp => wp.UserId);
                builder.HasIndex(wp => wp.FileId);
                builder.HasIndex(wp => wp.MediaType);
            });
            
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasMany<SubtitlePreference>().WithOne().HasForeignKey(sp => sp.UserId).OnDelete(DeleteBehavior.Cascade);
                builder.HasMany<WatchingProgress>().WithOne().HasForeignKey(wp => wp.UserId).OnDelete(DeleteBehavior.Cascade);
            });
            
            modelBuilder.Entity<User>()
                .Property(sl => sl.LibraryAccessIds)
                .HasConversion(
                    ls => string.Join(',', ls),
                    str => str.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList());

            if (_operationContext.Session?.IsAdmin == false)
            {
                modelBuilder.Entity<User>().HasQueryFilter(e => e.Id == _operationContext.Session.UserId);
                modelBuilder.Entity<SubtitlePreference>().HasQueryFilter(e => e.UserId == _operationContext.Session.UserId);
                modelBuilder.Entity<WatchingProgress>().HasQueryFilter(e => e.UserId == _operationContext.Session.UserId);
            }
        }

        private static void ConfigureFilmFileEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FilmFile>(builder =>
            {
                builder.HasIndex(ff => ff.PartOfLibraryId);
                builder.OwnsOne(ff => ff.MediaDetails);
                builder.HasOne(ff => ff.PartOfLibrary).WithMany().HasForeignKey(ff => ff.PartOfLibraryId).OnDelete(DeleteBehavior.Cascade);
                builder.HasOne(ff => ff.FilmDetails).WithMany().HasForeignKey(ff => ff.FilmDetailsId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(ff => ff.FilmDetails).WithMany().HasForeignKey(ff => ff.FilmDetailsId).OnDelete(DeleteBehavior.Restrict);
                builder.HasMany(ff => ff.Subtitles).WithOne().OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureEpisodeFileEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EpisodeFile>(builder =>
            {
                builder.HasIndex(ef => ef.SeasonNumber);
                builder.HasIndex(ef => ef.PartOfLibraryId);
                builder.HasOne(ef => ef.SeriesDetails).WithMany().OnDelete(DeleteBehavior.Restrict);
                builder.Ignore(ef => ef.SeasonDetails).Ignore(ef => ef.EpisodeDetails);
                builder.HasOne(ef => ef.PartOfLibrary).WithMany().OnDelete(DeleteBehavior.Cascade);
                builder.HasMany(ef => ef.Subtitles).WithOne().OnDelete(DeleteBehavior.Cascade);
                builder.OwnsOne(ef => ef.MediaDetails);
            });
        }
        
        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is ITrackedEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (ITrackedEntity)entityEntry.Entity;
                
                if (entityEntry.State == EntityState.Added)
                {
                    entity.Created = DateTime.UtcNow;
                    entity.CreatedCorrelationId = _operationContext.CorrelationId;
                }
                
                entity.Updated = DateTime.UtcNow;
                entity.UpdatedCorrelationId = _operationContext.CorrelationId;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

    public static class DbUtils
    {
        public static async Task AddOrUpdate<TEntity>(this DbContext context, IList<TEntity> entities)
            where TEntity : EntityBase
        {
            var set = context.Set<TEntity>();
            var ids = entities.Where(e => e.Id != default).Select(e => e.Id).ToList();
            var existing = await set.Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id);
            foreach (var entity in entities)
            {
                if (existing.TryGetValue(entity.Id, out var existingEntity))
                    context.Entry(existingEntity).CurrentValues.SetValues(entity);
                else
                    set.Add(entity);
            }
        }
    }
}