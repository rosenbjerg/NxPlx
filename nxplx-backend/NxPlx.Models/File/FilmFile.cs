using System.Collections.Generic;
using NxPlx.Models.Details;
using NxPlx.Models.Details.Film;
using FilmDetails = NxPlx.Models.Database.Film.FilmDetails;

namespace NxPlx.Models.File
{
    public class FilmFile : MediaFile
    {
        public string Title { get; set; }

        public FilmDetails FilmDetails { get; set; }
        
        public int FilmDetailsId { get; set; }
        
        public int Year { get; set; }
        
        public override string ToString()
        {
            return $"{Title} ({Year})";
        }
    }
}