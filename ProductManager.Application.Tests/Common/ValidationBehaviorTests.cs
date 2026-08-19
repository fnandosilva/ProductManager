using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using ProductManager.Application.Common.Behaviors;

namespace ProductManager.Application.Tests.Common;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>(Array.Empty<IValidator<SampleRequest>>());
        var nextCalled = false;

        var result = await behavior.Handle(new SampleRequest("test"), Next, CancellationToken.None);

        result.Should().Be("handled");
        nextCalled.Should().BeTrue();

        Task<string> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult("handled");
        }
    }

    [Fact]
    public async Task Handle_WithPassingValidators_ShouldCallNext()
    {
        var validator = new Mock<IValidator<SampleRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator.Object });

        var result = await behavior.Handle(
            new SampleRequest("test"),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        result.Should().Be("handled");
    }

    [Fact]
    public async Task Handle_WithFailingValidator_ShouldThrowValidationExceptionAndNotCallNext()
    {
        var failures = new List<ValidationFailure>
        {
            new(nameof(SampleRequest.Value), "Value is invalid.")
        };

        var validator = new Mock<IValidator<SampleRequest>>();
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator.Object });
        var nextCalled = false;

        var act = () => behavior.Handle(
            new SampleRequest("test"),
            _ =>
            {
                nextCalled = true;
                return Task.FromResult("handled");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldAggregateFailures()
    {
        var validator1 = new Mock<IValidator<SampleRequest>>();
        validator1
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Error from validator 1") }));

        var validator2 = new Mock<IValidator<SampleRequest>>();
        validator2
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<SampleRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Value", "Error from validator 2") }));

        var behavior = new ValidationBehavior<SampleRequest, string>(new[] { validator1.Object, validator2.Object });

        var act = () => behavior.Handle(new SampleRequest("test"), _ => Task.FromResult("handled"), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }
}
