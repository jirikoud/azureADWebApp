using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;

namespace AzureAADSource.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private NotificationHubClient notificationHub { get; set; }

        public NotificationController()
        {
            notificationHub = NotificationHubClient.CreateClientFromConnectionString(Environment.GetEnvironmentVariable("AZURE_NOTIFICATION_HUB_CONNECTION_STRING"), "cf-notification-hub");
        }

        // POST api/register
        // This creates a registration id
        [Route("Register")]
        [HttpPost]
        public async Task<string> Register(string? handle = null)
        {
            string? newRegistrationId = null;

            // make sure there are no existing registrations for this push handle (used for iOS and Android)
            if (handle != null)
            {
                var registrations = await notificationHub.GetRegistrationsByChannelAsync(handle, 100);

                foreach (RegistrationDescription registration in registrations)
                {
                    if (newRegistrationId == null)
                    {
                        newRegistrationId = registration.RegistrationId;
                    }
                    else
                    {
                        await notificationHub.DeleteRegistrationAsync(registration);
                    }
                }
            }
            newRegistrationId ??= await notificationHub.CreateRegistrationIdAsync();

            RegistrationDescription registrationDesc = new FcmRegistrationDescription(handle);
            registrationDesc.RegistrationId = newRegistrationId;
            registrationDesc.Tags = new HashSet<string>
            {
                "patientId:" + "12345"
            };

            await notificationHub.CreateOrUpdateRegistrationAsync(registrationDesc);
            return newRegistrationId;
        }

        // POST api/register
        // This creates a registration id
        [Route("Send")]
        [HttpPost]
        public async Task Send(string message)
        {
            var template = new Dictionary<string, string>();
            template.Add("message", message);
            var notification = new TemplateNotification(template);
            await notificationHub.SendNotificationAsync(notification);
        }

    }
}
