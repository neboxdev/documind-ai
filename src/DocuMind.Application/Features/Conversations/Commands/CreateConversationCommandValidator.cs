using FluentValidation;

namespace DocuMind.Application.Features.Conversations.Commands;

public class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty().WithMessage("Document ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Conversation title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
    }
}
