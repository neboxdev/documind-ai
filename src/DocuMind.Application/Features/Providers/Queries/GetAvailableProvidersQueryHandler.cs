using DocuMind.Application.DTOs;
using DocuMind.Application.Interfaces;
using MediatR;

namespace DocuMind.Application.Features.Providers.Queries;

public class GetAvailableProvidersQueryHandler
    : IRequestHandler<GetAvailableProvidersQuery, ProviderOutDTO[]>
{
    private readonly IAIProviderFactory _factory;

    public GetAvailableProvidersQueryHandler(IAIProviderFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public Task<ProviderOutDTO[]> Handle(GetAvailableProvidersQuery request, CancellationToken ct)
    {
        var providers = _factory.GetAvailableProviderDetails();
        return Task.FromResult(providers);
    }
}
