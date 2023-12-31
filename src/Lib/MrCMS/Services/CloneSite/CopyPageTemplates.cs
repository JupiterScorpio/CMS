using System.Threading.Tasks;
using MrCMS.DbConfiguration;
using MrCMS.Entities.Documents.Layout;
using MrCMS.Entities.Documents.Web;
using MrCMS.Entities.Multisite;
using MrCMS.Helpers;
using NHibernate;

namespace MrCMS.Services.CloneSite
{
    [CloneSitePart(-85)]
    public class CopyPageTemplates : ICloneSiteParts
    {
        private readonly ISession _session;

        public CopyPageTemplates(ISession session)
        {
            _session = session;
        }

        public async Task Clone(Site @from, Site to, SiteCloneContext siteCloneContext)
        {
            using (new SiteFilterDisabler(_session))
            {
                var existingTemplates = await
                    _session.QueryOver<PageTemplate>().Where(template => template.Site.Id == @from.Id).ListAsync();

                await _session.TransactAsync(async session =>
                {
                    foreach (var template in existingTemplates)
                    {
                        var copy = template.GetCopyForSite(to);
                        if (template.Layout != null)
                            copy.Layout = siteCloneContext.FindNew<Layout>(template.Layout.Id);
                        await session.SaveAsync(copy);
                        siteCloneContext.AddEntry(template, copy);
                    }
                });
            }
        }
    }
}