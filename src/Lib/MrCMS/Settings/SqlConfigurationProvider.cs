using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MrCMS.Entities.Settings;
using MrCMS.Helpers;
using MrCMS.Services;
using MrCMS.Settings.Events;
using MrCMS.Website.Caching;
using Newtonsoft.Json;
using NHibernate;

namespace MrCMS.Settings
{
    public class SqlConfigurationProvider : IConfigurationProvider
    {
        public const string SettingsDbCacheKey = "SiteSettingsDbCacheKey-{0}-{1}";
        private readonly IStatelessSession _session;

        private readonly ICurrentSiteLocator _siteLocator;

        // private readonly Site _site;
        private readonly ICacheManager _cacheManager;
        private readonly IEventContext _eventContext;

        public SqlConfigurationProvider(IStatelessSession session, ICurrentSiteLocator siteLocator,
            ICacheManager cacheManager,
            IEventContext eventContext)
        {
            _session = session;
            _siteLocator = siteLocator;
            // _site = site;
            _cacheManager = cacheManager;
            _eventContext = eventContext;
        }

        /// <summary>
        ///     Get Site Settings of the requested type
        /// </summary>
        /// <typeparam name="TSettings"></typeparam>
        /// <returns></returns>
        public virtual TSettings GetSiteSettings<TSettings>() where TSettings : SiteSettingsBase, new()
        {
            var site = _siteLocator.GetCurrentSite();
            var settingsDbCacheKey = string.Format(SettingsDbCacheKey, typeof(TSettings).FullName, site.Id);
            return _cacheManager.GetOrCreate(settingsDbCacheKey, () =>
            {
                var settings = Activator.CreateInstance<TSettings>();
                settings.SetSiteId(site.Id);

                var dbSettings = GetDbSettings<TSettings>();

                foreach (var prop in typeof(TSettings).GetProperties())
                {
                    // get properties we can read and write to
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;

                    var value = GetSettingByKey(dbSettings, prop.Name, prop.PropertyType);
                    if (value == null)
                        continue;


                    //set property
                    prop.SetValue(settings, value, null);
                }

                return settings;
            }, TimeSpan.FromMinutes(10), CacheExpiryType.Absolute);
        }

        public async Task SaveSettings(SiteSettingsBase settings)
        {
            var methodInfo = GetType().GetMethods().First(x => (x.Name == "SaveSettings") && x.IsGenericMethod);
            var genericMethod = methodInfo.MakeGenericMethod(settings.GetType());
            await genericMethod.InvokeVoidAsync(this, new object[] { settings });
        }

        public List<SiteSettingsBase> GetAllSiteSettings()
        {
            var methodInfo = GetType().GetMethodExt("GetSiteSettings");

            return TypeHelper.GetAllConcreteTypesAssignableFrom<SiteSettingsBase>()
                .Select(type => methodInfo.MakeGenericMethod(type).Invoke(this, new object[] { }))
                .OfType<SiteSettingsBase>().ToList();
        }

        /// <summary>
        ///     Save settings object
        /// </summary>
        /// <typeparam name="TSettings">Type</typeparam>
        /// <param name="settings">Setting instance</param>
        public virtual async Task SaveSettings<TSettings>(TSettings settings) where TSettings : SiteSettingsBase, new()
        {
            var existing = GetSiteSettings<TSettings>();
            var existingInDb = GetDbSettings<TSettings>();
            await _session.TransactAsync(async session =>
            {
                /* We do not clear cache after each setting update.
                 * This behavior can increase performance because cached settings will not be cleared 
                 * and loaded from database after each update */
                foreach (var prop in typeof(TSettings).GetProperties())
                {
                    // get properties we can read and write to
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;

                    var typeName = typeof(TSettings).FullName;
                    dynamic value = prop.GetValue(settings, null);
                    await SetSetting(session, existingInDb, typeName, prop.Name, value ?? "");
                }
            });
            ClearCache();
            if (_eventContext != null)
                await _eventContext.Publish<IOnSavingSiteSettings<TSettings>, OnSavingSiteSettingsArgs<TSettings>>(
                    new OnSavingSiteSettingsArgs<TSettings>(settings, existing));
        }

        /// <summary>
        ///     Delete all settings
        /// </summary>
        /// <typeparam name="TSettings">Type</typeparam>
        public virtual async Task DeleteSettings<TSettings>(TSettings settings)
            where TSettings : SiteSettingsBase, new()
        {
            var allSettings = GetDbSettings<TSettings>().Values;

            foreach (var setting in allSettings)
                await DeleteSetting(setting);

            ClearCache();
        }

        /// <summary>
        ///     Clear cache
        /// </summary>
        public virtual void ClearCache()
        {
            _cacheManager.Clear();
        }


        private IDictionary<string, Setting> GetDbSettings<TSettings>() where TSettings : SiteSettingsBase, new()
        {
            var site = _siteLocator.GetCurrentSite();
            var typeName = typeof(TSettings).FullName.ToLower();
            var settings = _session.QueryOver<Setting>()
                .Where(x => x.SettingType == typeName && x.Site.Id == site.Id)
                .List();
            return settings.GroupBy(setting => setting.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(y => y).First());
        }

        /// <summary>
        ///     Adds a setting
        /// </summary>
        /// <param name="session"></param>
        /// <param name="setting">Setting</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual async Task InsertSetting(IStatelessSession session, Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            setting.Site = _siteLocator.GetCurrentSite();
            await session.InsertAsync(setting);
            ClearCache();
        }

        /// <summary>
        ///     Updates a setting
        /// </summary>
        /// <param name="session1"></param>
        /// <param name="setting">Setting</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual async Task UpdateSetting(IStatelessSession session, Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            await session.UpdateAsync(setting);
            ClearCache();
        }

        /// <summary>
        ///     Deletes a setting
        /// </summary>
        /// <param name="setting">Setting</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected virtual async Task DeleteSetting(Setting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            await _session.TransactAsync(session => session.DeleteAsync(setting));
            ClearCache();
        }

        /// <summary>
        ///     Get setting value by key
        /// </summary>
        /// <param name="existingSettings"></param>
        /// <param name="propertyName">Key</param>
        /// <param name="type">value type</param>
        /// <param name="defaultValue">Default value</param>
        /// <returns>Setting value</returns>
        protected virtual object GetSettingByKey(IDictionary<string, Setting> existingSettings, string propertyName,
            Type type, object defaultValue = null)
        {
            if (string.IsNullOrEmpty(propertyName))
                return defaultValue;

            propertyName = Standardise(propertyName);
            if (existingSettings.ContainsKey(propertyName))
            {
                var setting = existingSettings[propertyName];
                if (setting != null)
                    return JsonConvert.DeserializeObject(setting.Value, type);
            }

            return defaultValue;
        }

        protected virtual async Task SetSetting<T>(IStatelessSession session,
            IDictionary<string, Setting> existingSettings,
            string typeName,
            string propertyName, T value)
        {
            typeName = Standardise(typeName);
            propertyName = Standardise(propertyName);
            var valueStr = JsonConvert.SerializeObject(value);

            var setting = existingSettings.ContainsKey(propertyName) ? existingSettings[propertyName] : null;
            if (setting != null)
            {
                //update
                setting.Value = valueStr;
                setting.UpdatedOn = DateTime.UtcNow;
                await UpdateSetting(session, setting);
            }
            else
            {
                //insert
                var site = _siteLocator.GetCurrentSite();
                setting = new Setting
                {
                    SettingType = typeName,
                    PropertyName = propertyName,
                    Value = valueStr,
                    Site = site,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                await InsertSetting(session, setting);
            }

            ClearCache();
        }

        private string Standardise(string value)
        {
            return value?.Trim().ToLowerInvariant();
        }
    }
}