using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace D365MarketingForms.Server;

public static class OrganizationServiceExtensions
{
    public static async Task<WhoAmIResponse> WhoAmIAsync(this IOrganizationServiceAsync2 service, CancellationToken cancellationToken = default)
    {
        return service == null
            ? throw new ArgumentNullException(nameof(service))
            : await service.ExecuteAsync<WhoAmIResponse>(new WhoAmIRequest(), cancellationToken);
    }

    public static async Task<TResponse> ExecuteAsync<TResponse>(this IOrganizationServiceAsync2 service, OrganizationRequest request, CancellationToken cancellationToken = default)
       where TResponse : OrganizationResponse
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var response = await service.ExecuteAsync(request, cancellationToken);

        if (response is TResponse typedResponse)
        {
            return typedResponse;
        }

        throw new InvalidCastException($"Cannot cast response of type '{response.GetType().FullName}' to '{typeof(TResponse).FullName}'");
    }
}
