﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.Helpers;
using MrCMS.Settings;

namespace MrCMS.Website.Filters
{
    public class TaskExecutionKeyPasswordAuth : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsLocal())
                return;
            var siteSettings = filterContext.HttpContext.RequestServices.GetRequiredService<SiteSettings>();
            string item = filterContext.HttpContext.Request.Query[siteSettings.TaskExecutorKey];
            if (string.IsNullOrWhiteSpace(item) || item != siteSettings.TaskExecutorPassword)
                filterContext.Result = new EmptyResult();
            //filterContext.HttpContext.Server.ScriptTimeout = 6000;
        }
    }
}