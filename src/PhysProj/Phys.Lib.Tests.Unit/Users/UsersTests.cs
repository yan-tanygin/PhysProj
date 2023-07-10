using Phys.Lib.Core.Users;

namespace Phys.Lib.Tests.Unit.Users
{
    public class UsersTests
    {
        private static readonly UserValidators.NameValidator userNameValidator = new UserValidators.NameValidator();

        [Theory]
        [InlineData("ya", false)]
        [InlineData("8lalala", false)]
        [InlineData(" admin", false)]
        [InlineData("admin ", false)]
        [InlineData("ad$min", false)]
        [InlineData("admin", true)]
        public void TestUserName(string userName, bool isValid)
        {
            var result = userNameValidator.Validate(userName);
            result.IsValid.Should().Be(isValid);
        }
    }
}