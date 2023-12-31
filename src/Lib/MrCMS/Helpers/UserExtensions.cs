using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MrCMS.Entities.People;

namespace MrCMS.Helpers
{
    public static class UserExtensions
    {
        public static CultureInfo GetUICulture(this User user, CultureInfo defaultCultureInfo = null)
        {
            try
            {
                defaultCultureInfo = defaultCultureInfo ?? CultureInfo.CurrentCulture; 
                if (user == null || string.IsNullOrWhiteSpace(user.UICulture))
                    return defaultCultureInfo;
                return CultureInfo.GetCultureInfo(user.UICulture);
            }
            catch
            {
                return defaultCultureInfo;
            }
        }

        public static T Get<T>(this User user) where T : UserProfileData
        {
            return user?.UserProfileData.OfType<T>().FirstOrDefault();
        }

        public static T2 Get<T1, T2>(this User user, Func<T1, T2> func) where T1 : UserProfileData
        {
            T1 firstOrDefault = user.UserProfileData.OfType<T1>().FirstOrDefault();
            return firstOrDefault == null ? default(T2) : func(firstOrDefault);
        }

        public static IEnumerable<T> GetAll<T>(this User user) where T : UserProfileData
        {
            return user.UserProfileData.OfType<T>();
        }
    }
}