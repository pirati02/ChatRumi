using ChatRumi.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ChatRumi.Infrastructure;

public static class AttachmentFileStorageExtensions
{
    public static IServiceCollection AddAttachmentFileStorage(this IServiceCollection services)
    {
        services.AddSingleton<IAttachmentFileStorage, LocalAttachmentFileStorage>();
        return services;
    }
}
