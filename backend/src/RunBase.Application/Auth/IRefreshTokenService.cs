using RunBase.Domain.Users;

namespace RunBase.Application.Auth;

public interface IRefreshTokenService
{
    RefreshToken Create(User user);
}
