using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Notification.Application;

public static class ModuleRegistration
{
    extension(IServiceCollection services)
    {
        public void AddApplication()
        {
            services.AddScoped<INotificationService, NotificationService>();
        }
    }
}
