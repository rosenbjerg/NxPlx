using System.Collections.Generic;
using NxPlx.Domain.Models.Database;

namespace NxPlx.Domain.Models.Details.Film
{
    public class MovieCollection : EntityBase, IPosterImageOwner, IBackdropImageOwner
    {
        public string Name { get; set; }
        public string PosterPath { get; set; }
        public string PosterBlurHash { get; set; }
        public string BackdropPath { get; set; }
        public string BackdropBlurHash { get; set; }

        public virtual ICollection<DbFilmDetails> Movies { get; set; }
    }
}