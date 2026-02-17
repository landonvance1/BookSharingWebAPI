using BookSharingApp.Common;
using BookSharingWebAPI.Validators;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace BookSharingApp.Tests.Validators
{
    public class CoverImageValidatorTests
    {
        private readonly CoverImageValidator _validator = new();

        private static Mock<IFormFile> CreateFileMock(
            long length = 1024,
            string contentType = "image/jpeg")
        {
            var mock = new Mock<IFormFile>();
            mock.Setup(f => f.Length).Returns(length);
            mock.Setup(f => f.ContentType).Returns(contentType);
            return mock;
        }

        public class Validate_NullOrEmptyFile : CoverImageValidatorTests
        {
            [Fact]
            public void Validate_WithNullFile_ReturnsFailure()
            {
                var result = _validator.Validate(null);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Be("No image file provided");
            }

            [Fact]
            public void Validate_WithZeroLengthFile_ReturnsFailure()
            {
                var file = CreateFileMock(length: 0);

                var result = _validator.Validate(file.Object);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Be("No image file provided");
            }
        }

        public class Validate_FileSize : CoverImageValidatorTests
        {
            [Fact]
            public void Validate_WithFileThatExceedsMaxSize_ReturnsFailure()
            {
                var oversizedFile = CreateFileMock(
                    length: ImageAnalysisConstants.MaxImageFileSizeBytes + 1);

                var result = _validator.Validate(oversizedFile.Object);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("too large");
                result.ErrorMessage.Should().Contain("MB");
            }

            [Fact]
            public void Validate_WithFileAtExactMaxSize_ReturnsSuccess()
            {
                var file = CreateFileMock(
                    length: ImageAnalysisConstants.MaxImageFileSizeBytes);

                var result = _validator.Validate(file.Object);

                result.IsValid.Should().BeTrue();
            }
        }

        public class Validate_ContentType : CoverImageValidatorTests
        {
            [Theory]
            [InlineData("image/jpeg")]
            [InlineData("image/png")]
            [InlineData("image/webp")]
            public void Validate_WithSupportedContentType_ReturnsSuccess(string contentType)
            {
                var file = CreateFileMock(contentType: contentType);

                var result = _validator.Validate(file.Object);

                result.IsValid.Should().BeTrue();
            }

            [Theory]
            [InlineData("image/gif")]
            [InlineData("application/pdf")]
            [InlineData("text/plain")]
            [InlineData("")]
            public void Validate_WithUnsupportedContentType_ReturnsFailure(string contentType)
            {
                var file = CreateFileMock(contentType: contentType);

                var result = _validator.Validate(file.Object);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Invalid image type");
            }

            [Fact]
            public void Validate_WithNullContentType_ReturnsFailure()
            {
                var file = new Mock<IFormFile>();
                file.Setup(f => f.Length).Returns(1024);
                file.Setup(f => f.ContentType).Returns((string)null!);

                var result = _validator.Validate(file.Object);

                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Invalid image type");
            }
        }
    }
}
