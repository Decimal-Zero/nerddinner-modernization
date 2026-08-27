using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NerdDinner.Models;
using Xunit;

namespace NerdDinner.Tests.Models
{
    public class AccountModelsTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var context = new ValidationContext(model);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        // --- RegisterModel: password confirmation logic is core to the
        // registration flow that M4 (auth stack replacement) will need to
        // preserve, so it's worth pinning down precisely. ---

        [Fact]
        public void RegisterModel_Passes_WithMatchingPasswordsAtMinimumLength()
        {
            var model = new RegisterModel
            {
                UserName = "alice",
                Password = "abcdef", // exactly 6 chars, the MinimumLength
                ConfirmPassword = "abcdef"
            };

            Assert.Empty(Validate(model));
        }

        [Fact]
        public void RegisterModel_Fails_WhenPasswordBelowMinimumLength()
        {
            var model = new RegisterModel
            {
                UserName = "alice",
                Password = "abcde", // 5 chars, below MinimumLength of 6
                ConfirmPassword = "abcde"
            };

            var results = Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterModel.Password)));
        }

        [Fact]
        public void RegisterModel_Fails_WhenConfirmPasswordDoesNotMatch()
        {
            var model = new RegisterModel
            {
                UserName = "alice",
                Password = "abcdef",
                ConfirmPassword = "different"
            };

            var results = Validate(model);

            Assert.Contains(results, r => r.ErrorMessage.Contains("password and confirmation password do not match."));
        }

        [Fact]
        public void RegisterModel_Fails_WhenUserNameMissing()
        {
            var model = new RegisterModel
            {
                UserName = null,
                Password = "abcdef",
                ConfirmPassword = "abcdef"
            };

            var results = Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterModel.UserName)));
        }

        // --- LoginModel: minimal validation (only presence, no length
        // rule on the password -- unlike RegisterModel). Worth pinning
        // down that asymmetry explicitly. ---

        [Fact]
        public void LoginModel_Passes_WithAnyNonEmptyPassword()
        {
            // Deliberately a single-character password: LoginModel has no
            // [StringLength]/MinimumLength on Password, unlike
            // RegisterModel. This is expected -- login checks against
            // whatever was actually registered, it doesn't re-enforce
            // password policy. Documented here so that asymmetry isn't
            // "fixed" by accident later.
            var model = new LoginModel { UserName = "alice", Password = "x" };

            Assert.Empty(Validate(model));
        }

        [Fact]
        public void LoginModel_Fails_WhenPasswordMissing()
        {
            var model = new LoginModel { UserName = "alice", Password = null };

            var results = Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LoginModel.Password)));
        }

        // --- LocalPasswordModel: same Compare-attribute pattern as
        // RegisterModel, used on the "change password" flow. ---

        [Fact]
        public void LocalPasswordModel_Fails_WhenConfirmPasswordDoesNotMatchNewPassword()
        {
            var model = new LocalPasswordModel
            {
                OldPassword = "oldpass",
                NewPassword = "newpass",
                ConfirmPassword = "different"
            };

            var results = Validate(model);

            Assert.Contains(results, r => r.ErrorMessage.Contains("password and confirmation password do not match."));
        }

        [Fact]
        public void LocalPasswordModel_Fails_WhenNewPasswordBelowMinimumLength()
        {
            var model = new LocalPasswordModel
            {
                OldPassword = "oldpass",
                NewPassword = "short",
                ConfirmPassword = "short"
            };

            var results = Validate(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(LocalPasswordModel.NewPassword)));
        }
    }
}
