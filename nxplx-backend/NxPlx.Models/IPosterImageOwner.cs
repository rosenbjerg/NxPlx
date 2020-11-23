﻿namespace NxPlx.Models
{
    public interface IPosterImageOwner : IImageOwner
    {
        public string PosterPath { get; set; }
        public string PosterBlurHash { get; set; }
    }
}