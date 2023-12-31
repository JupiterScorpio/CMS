using System.Collections.Generic;
using System.Threading.Tasks;
using MrCMS.Settings;

namespace MrCMS.HealthChecks
{
    public class LoginNotificationEmailsEnabled : HealthCheck
    {
        private readonly SecuritySettings _settings;

        public LoginNotificationEmailsEnabled(SecuritySettings settings)
        {
            _settings = settings;
        }

        public override string DisplayName => "Login Notification Emails Enabled";

        public override Task<HealthCheckResult> PerformCheck()
        {

            return Task.FromResult(_settings.SendLoginNotificationEmails
                ? HealthCheckResult.Success
                : new HealthCheckResult
                {
                    Status = HealthCheckStatus.Warning,
                    Messages = new List<string>
                    {
                        "Login notification emails have not been enabled for this site."
                    }
                });
        }
    }
}