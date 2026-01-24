using Consul;
using Ocelot.Provider.Consul.Interfaces;
using Ocelot.Values;

namespace ChatRum.Gateway;

/// <summary>
/// Custom Consul service builder that always uses ServiceAddress instead of Node address.
/// This fixes the Docker container ID issue where Ocelot would use Node.Address (container ID)
/// instead of ServiceAddress (Docker service name).
/// </summary>
public sealed class ChatRumConsulServiceBuilder : IConsulServiceBuilder
{
    public IEnumerable<Service> BuildServices(ServiceEntry[] entries, Node[] nodes)
    {
        return entries
            .Where(IsValid)
            .Select(entry => 
            {
                Node entryNode = entry.Node;
                var entryNodeName = entryNode.Name;
                var matchedNode = nodes.FirstOrDefault(n => n.Name == entryNodeName);
                return CreateService(entry, matchedNode);
            });
    }
    
    public Service CreateService(ServiceEntry entry, Node? node)
    {
        // CRITICAL: Always prefer ServiceAddress over Node Address
        // This is the fix for Docker environments where ServiceAddress contains
        // the Docker service name (e.g., "account-service") that Docker DNS can resolve
        Node entryNode = entry.Node;
        var address = !string.IsNullOrEmpty(entry.Service.Address) 
            ? entry.Service.Address 
            : entryNode.Address;
            
        return new Service(
            entry.Service.Service,
            new ServiceHostAndPort(address, entry.Service.Port),
            entry.Service.ID,
            string.Empty,
            entry.Service.Tags ?? []);
    }
    
    public bool IsValid(ServiceEntry entry)
    {
        // Service is valid if it has checks and all checks are passing
        return entry.Checks == null || 
               entry.Checks.Length == 0 || 
               entry.Checks.All(c => c.Status.Status == "passing");
    }
}