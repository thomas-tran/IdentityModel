using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ScottBrady.IdentityModel.AspNetCore.Identity;
using Xunit;

namespace ScottBrady.IdentityModel.Tests.AspNetCore.Identity
{
    public class ExtendedPasswordValidatorTests
    {
        private ExtendedPasswordValidator<IdentityUser> CreateSut() => new ExtendedPasswordValidator<IdentityUser>();

        private Mock<ExtendedPasswordValidator<IdentityUser>> CreateMockedSut()
        {
            var sut = new Mock<ExtendedPasswordValidator<IdentityUser>>(null) {CallBase = true};
            sut.Setup(x => x.BaseValidate(It.IsAny<UserManager<IdentityUser>>(), It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            sut.Setup(x => x.HasConsecutiveCharacters(It.IsAny<string>(), It.IsAny<int>())).Returns(false);
            return sut;
        }

        [Fact]
        public async Task ValidateAsync_WhenUserManagerIsNull_ExpectArgumentNullException()
        {
            var sut = CreateMockedSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Object.ValidateAsync(null, new IdentityUser(), "password"));
        }

        [Fact]
        public async Task ValidateAsync_WhenPasswordIsNull_ExpectArgumentNullException()
        {
            var sut = CreateMockedSut();
            await Assert.ThrowsAsync<ArgumentNullException>(() => sut.Object.ValidateAsync(CreateMockUserManager().Object, new IdentityUser(), null));
        }

        [Fact]
        public async Task ValidateAsync_WhenBaseValidationFails_ExpectErrorsIncludedInResult()
        {
            var expectedError = new IdentityError {Code = "22", Description = "oh no!"};
            
            var sut = CreateMockedSut();
            sut.Setup(x => x.BaseValidate(It.IsAny<UserManager<IdentityUser>>(), It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(expectedError));

            var result = await sut.Object.ValidateAsync(CreateMockUserManager().Object, new IdentityUser(), "pass");

            result.Errors.Should().Contain(expectedError);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void HasConsecutiveCharacters_WhenPasswordIsNullOrWhitespace_ExpectArgumentNullException(string password)
        {
            var sut = CreateSut();
            Assert.Throws<ArgumentNullException>(() => sut.HasConsecutiveCharacters(password, 42));
        }
        
        [Fact]
        public void HasConsecutiveCharacters_WhenNoConsecutiveCharacters_ExpectFalse()
        {
            const int maxConsecutiveCharacters = 1;
            const string password = "qwertyuiopasdfghjklzxcvbnm";

            var sut = CreateSut();
            var hasConsecutiveCharacters = sut.HasConsecutiveCharacters(password, maxConsecutiveCharacters);

            hasConsecutiveCharacters.Should().BeFalse();
        }
        
        [Fact]
        public void HasConsecutiveCharacters_WhenConsecutiveCharactersButUnderLimit_ExpectFalse()
        {
            const int maxConsecutiveCharacters = 2;
            const string password = "qqwweerrttyy";

            var sut = CreateSut();
            var hasConsecutiveCharacters = sut.HasConsecutiveCharacters(password, maxConsecutiveCharacters);

            hasConsecutiveCharacters.Should().BeFalse();
        }
        
        [Fact]
        public void HasConsecutiveCharacters_WhenConsecutiveCharactersAndOverLimit_ExpectTrue()
        {
            const int maxConsecutiveCharacters = 1;
            const string password = "qqwertyy";

            var sut = CreateSut();
            var hasConsecutiveCharacters = sut.HasConsecutiveCharacters(password, maxConsecutiveCharacters);

            hasConsecutiveCharacters.Should().BeTrue();
        }

        private static Mock<UserManager<IdentityUser>> CreateMockUserManager()
            => new Mock<UserManager<IdentityUser>>(new Mock<IUserStore<IdentityUser>>().Object, null, null, null, null, null, null, null, null);
    }
}