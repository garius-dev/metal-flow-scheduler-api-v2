using AutoMapper;
using MetalFlowScheduler.Api.Application.Dtos.Auth;
using MetalFlowScheduler.Api.Application.Interfaces;
using MetalFlowScheduler.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity; // Necessário para UserManager e SignInManager
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq; // Necessário para Select
using Microsoft.Extensions.Logging; // Necessário para ILogger

namespace MetalFlowScheduler.Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger; // Adicionado logger

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthService> logger) // Injetar logger
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Inicializar logger
        }

        /// <inheritdoc/>
        public async Task<IdentityResult> RegisterAsync(RegisterRequestDto request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                // Outras propriedades customizadas podem ser mapeadas aqui, se houver
                // FullName = request.FullName,
                // IsActive = true // Identity já gerencia status com LockoutEnabled
            };

            // O UserManager cuida do hashing da senha de forma segura
            var result = await _userManager.CreateAsync(user, request.Password);

            // Opcional: Atribuir uma role padrão ao novo usuário
            // if (result.Succeeded)
            // {
            //     if (!string.IsNullOrEmpty(request.Role))
            //     {
            //         // Crie a role se ela não existir
            //         if (!await _userManager.IsInRoleAsync(user, request.Role))
            //         {
            //              // Você precisaria de um RoleManager injetado para criar roles
            //              // var roleExists = await _roleManager.RoleExistsAsync(request.Role);
            //              // if (!roleExists) await _roleManager.CreateAsync(new ApplicationRole(request.Role));
            //              // await _userManager.AddToRoleAsync(user, request.Role);
            //         }
            //     }
            //     else
            //     {
            //          // Atribuir uma role padrão se nenhuma for especificada
            //          // await _userManager.AddToRoleAsync(user, "User");
            //     }
            // }

            return result;
        }

        /// <inheritdoc/>
        public async Task<LoginResponseDto?> AuthenticateAsync(string username, string password)
        {
            // O SignInManager cuida da validação da senha (comparando o hash) e verifica status do usuário (bloqueio, etc.)
            var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                // Log tentativas de login falhas para monitoramento de segurança
                _logger.LogWarning("Falha na autenticação para o usuário: {Username}. Resultado: {SignInResult}", username, result);
                return null; // Autenticação falhou
            }

            // Autenticação bem-sucedida, buscar o usuário para gerar o token
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                _logger.LogError("Usuário {Username} autenticado com sucesso, mas não encontrado no UserManager. Isso não deveria acontecer.", username);
                return null; // Falha inesperada
            }

            // Gerar JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var expirationTime = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationInMinutes"]));

            // Obter claims do usuário, incluindo roles
            var claims = (await _userManager.GetClaimsAsync(user)).ToList();
            claims.AddRange(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID do usuário
                new Claim(ClaimTypes.Name, user.UserName), // Nome de usuário
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject (comum em JWT)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID
                // Adicionar outras claims importantes
            });

            // Adicionar claims de roles
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expirationTime,
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponseDto
            {
                Token = tokenString,
                UserId = user.Id, // Identity usa Id (int) por padrão com IdentityUser<int>
                Username = user.UserName
            };
        }

        // Opcional: Implementar outros métodos da interface IAuthService
        // public async Task<bool> AssignRoleAsync(string userId, string roleName) { ... }
        // public async Task<IEnumerable<string>> GetUserRolesAsync(string userId) { ... }
    }
}
