using System;
using System.Collections.Generic;

namespace MrCMS.Services.Sitemaps
{
    public interface ISitemapDataSource
    {
        IEnumerable<SitemapData> GetAdditionalData();
        IEnumerable<Type> TypesToRemove { get; }
        
    }

    public interface ISitemapPagesToRemove
    {
        IEnumerable<int> WebpageIds { get; }
    }
}