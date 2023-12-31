﻿using System.Threading.Tasks;
using MrCMS.Settings;

namespace MrCMS.Messages.Security
{
    public class GetDefaultFooterScriptChangeMessageTemplate: GetDefaultTemplate<FooterScriptChangeMessageTemplate>
    {
        private readonly MailSettings _mailSettings;

        public GetDefaultFooterScriptChangeMessageTemplate(MailSettings mailSettings)
        {
            _mailSettings = mailSettings;
        }
        public override Task<FooterScriptChangeMessageTemplate> Get()
        {
            return Task.FromResult(new FooterScriptChangeMessageTemplate
            {
                Subject = "Footer Script Change - {Status}",
                FromAddress = _mailSettings.SystemEmailAddress,
                ToAddress = _mailSettings.SystemEmailAddress,
                Body = "The page {Name} ({Url}) has been modified."
            });
        }
    }
}