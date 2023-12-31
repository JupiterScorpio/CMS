﻿using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using MrCMS.Entities.Documents.Web;

namespace MrCMS.DbConfiguration.Overrides
{
    public class UrlHistoryOverride : IAutoMappingOverride<UrlHistory>
    {
        public void Override(AutoMapping<UrlHistory> mapping)
        {
            mapping.Map(x => x.UrlSegment).Length(500).Index("idx_UrlSegment_UrlHistory");
            mapping.Map(x => x.RedirectUrl).Length(500).Index("idx_UrlSegment_RedirectUrl");
        }
    }
}
