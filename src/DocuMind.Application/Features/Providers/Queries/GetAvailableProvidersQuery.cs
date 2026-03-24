using DocuMind.Application.DTOs;
using MediatR;

namespace DocuMind.Application.Features.Providers.Queries;

public record GetAvailableProvidersQuery : IRequest<ProviderOutDTO[]>;
