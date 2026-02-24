using MediatR;
using BikMetric.Application.Common.Interfaces;

namespace BikMetric.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IDapperRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IDapperRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.QueryFirstOrDefaultAsync<UserLoginDto>(
            "SELECT id, email, full_name, password_hash, role, is_active FROM users WHERE email = @Email",
            new { Email = request.Email.ToLowerInvariant() });

        if (user == null)
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Kullanıcı hesabı aktif değil");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Geçersiz e-posta veya şifre");

        var token = _jwtTokenGenerator.GenerateToken(
            userId: user.Id,
            email: user.Email,
            fullName: user.FullName,
            role: user.Role);

        return new LoginResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Token = token
        };
    }

    private class UserLoginDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
