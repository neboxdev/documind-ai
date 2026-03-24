using DocuMind.Application.Features.Documents.Commands;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DocuMind.UnitTests;

public class UploadDocumentCommandValidatorTests
{
    private readonly UploadDocumentCommandValidator _validator = new();

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("text/plain")]
    public void Validate_ValidContentType_Passes(string contentType)
    {
        var command = new UploadDocumentCommand(
            new MemoryStream([1, 2, 3]),
            "test.pdf",
            contentType,
            1024);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContentType);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("application/json")]
    [InlineData("video/mp4")]
    public void Validate_InvalidContentType_Fails(string contentType)
    {
        var command = new UploadDocumentCommand(
            new MemoryStream([1, 2, 3]),
            "test.png",
            contentType,
            1024);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Fact]
    public void Validate_FileTooLarge_Fails()
    {
        var command = new UploadDocumentCommand(
            new MemoryStream([1]),
            "huge.pdf",
            "application/pdf",
            11 * 1024 * 1024); // 11 MB

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Size);
    }

    [Fact]
    public void Validate_EmptyFileName_Fails()
    {
        var command = new UploadDocumentCommand(
            new MemoryStream([1, 2, 3]),
            "",
            "application/pdf",
            1024);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void Validate_ZeroSize_Fails()
    {
        var command = new UploadDocumentCommand(
            new MemoryStream(),
            "empty.pdf",
            "application/pdf",
            0);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Size);
    }
}
