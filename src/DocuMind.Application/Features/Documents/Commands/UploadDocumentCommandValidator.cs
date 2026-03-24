using FluentValidation;

namespace DocuMind.Application.Features.Documents.Commands;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/msword",
        "text/plain"
    ];

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.");

        RuleFor(x => x.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Only PDF, DOCX, and TXT files are accepted.");

        RuleFor(x => x.Size)
            .GreaterThan(0).WithMessage("File cannot be empty.")
            .LessThanOrEqualTo(MaxFileSize).WithMessage("File size must not exceed 10 MB.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File content is required.");
    }
}
