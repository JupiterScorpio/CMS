﻿using System;
using MrCMS.Helpers;
using MrCMS.Services;
using NHibernate;

namespace MrCMS.DbConfiguration
{
    public class SiteFilterDisabler : IDisposable
    {
        private readonly ISession _session;
        private readonly bool _filterEnabled;

        public SiteFilterDisabler(ISession session)
        {
            _session = session;

            _filterEnabled = _session.GetEnabledFilter("SiteFilter") != null;
            if (_filterEnabled)
            {
                _session.DisableFilter("SiteFilter");
            }
        }

        public void Dispose()
        {
            if (_filterEnabled)
            {
                _session.EnableFilter("SiteFilter").SetParameter("site", _session.GetService<ICurrentSiteLocator>().GetCurrentSite().Id);
            }
        }
    }
}