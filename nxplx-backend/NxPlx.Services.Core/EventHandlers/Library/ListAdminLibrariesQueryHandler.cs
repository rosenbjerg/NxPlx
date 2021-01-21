﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NxPlx.Application.Models;
using NxPlx.Application.Models.Events.Library;
using NxPlx.Infrastructure.Database;

namespace NxPlx.Core.Services.EventHandlers.Library
{
    public class ListAdminLibrariesQueryHandler : IEventHandler<ListAdminLibrariesQuery, List<AdminLibraryDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IMapper _mapper;

        public ListAdminLibrariesQueryHandler(DatabaseContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public Task<List<AdminLibraryDto>> Handle(ListAdminLibrariesQuery query, CancellationToken cancellationToken = default)
        {
            return _context.Libraries.ProjectTo<AdminLibraryDto>(_mapper.ConfigurationProvider).ToListAsync();
        }
    }
}