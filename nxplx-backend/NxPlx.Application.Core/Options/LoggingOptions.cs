﻿using System.ComponentModel.DataAnnotations;
using Serilog.Events;

namespace NxPlx.Application.Core.Options
{
    public class LoggingOptions : INxplxOptions
    {
        [Required]
        public LogEventLevel LogLevel { get; set; }

        public string Seq { get; set; } = null!;
    }
}