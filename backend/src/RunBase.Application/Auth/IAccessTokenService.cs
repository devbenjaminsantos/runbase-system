using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public interface IAccessTokenService
{
    AccessToken Create(User user);
}
