using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using NxPlx.Models.Details.Film;
using NxPlx.Models.Details.Search;
using NxPlx.Models.Details.Series;

namespace NxPlx.Abstractions
{
    public abstract class DetailsApiBase : IDetailsApi
    {
        protected readonly ICachingService CachingService;
        protected IDetailsMapper Mapper;
        protected ILoggingService LoggingService;
        private string _imageFolder;
        
        private readonly HttpClient Client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                {"User-Agent", "NxPlx"}
            }
        };

        protected DetailsApiBase(string imageFolder, ICachingService cachingService, IDetailsMapper mapper, ILoggingService loggingService)
        {
            CachingService = cachingService;
            LoggingService = loggingService;
            Mapper = mapper;
            _imageFolder = imageFolder;
        }
        
        protected async Task<(string? content, bool cached)> FetchInternal(string url)
        {
            var cached = true;
            var content = await CachingService.GetAsync(url);

            if (string.IsNullOrEmpty(content))
            {
                var response = await Client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    content = await response.Content.ReadAsStringAsync();
                    await CachingService.SetAsync(url, content, CacheKind.WebRequest);
                }
                cached = false;

            }

            return (content, cached);
        }

        protected async Task DownloadImageInternal(string url, string size, string imageName)
        {
            var sizeDir = Path.Combine(_imageFolder, size);
            var outputPath = Path.Combine(Path.Combine(sizeDir, $"{imageName}.jpg"));
            Directory.CreateDirectory(sizeDir);
            
            if (!File.Exists(outputPath))
            {
                var response = await Client.GetAsync(url);
                using (var imageStream = await response.Content.ReadAsStreamAsync())
                {
                    try
                    {
                        using (var outputStream = File.OpenWrite(outputPath))
                        {
                            await imageStream.CopyToAsync(outputStream);
                        }
                    }
                    catch (IOException)
                    {
                        LoggingService.Trace("Failed to download image {ImagePath}. It is already being downloaded", outputPath);
                    }
                }
                
            }
        }
        
        public abstract Task<FilmResult[]> SearchMovies(string title, int year);
        public abstract Task<SeriesResult[]> SearchTvShows(string name);
        public abstract Task<FilmDetails> FetchMovieDetails(int id, string language);
        public abstract Task<SeriesDetails> FetchTvDetails(int id, string language);
        public abstract Task<SeasonDetails> FetchTvSeasonDetails(int id, int season, string language);
        public abstract Task DownloadImage(string size, string imageUrl);
    }
}