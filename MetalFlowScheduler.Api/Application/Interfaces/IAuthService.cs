using MetalFlowScheduler.Api.Application.Dtos.Auth;
using Microsoft.AspNetCore.Identity; // Necessário para IdentityResult
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de autenticação e gerenciamento de usuários.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registra um novo usuário.
        /// </summary>
        /// <param name="request">Dados de registro.</param>
        /// <returns>Resultado da operação de criação do usuário.</returns>
        Task<IdentityResult> RegisterAsync(RegisterRequestDto request);

        /// <summary>
        /// Valida as credenciais do usuário e, se válidas, gera um JWT.
        /// </summary>
        /// <param name="username">Nome de usuário ou email.</param>
        /// <param name="password">Senha.</param>
        /// <returns>O JWT e informações básicas do usuário se a autenticação for bem-sucedida, caso contrário, null.</returns>
        Task<LoginResponseDto?> AuthenticateAsync(string username, string password);

        // Opcional: Adicionar métodos para gerenciar roles, claims, reset de senha, etc.
        // Task<bool> AssignRoleAsync(string userId, string roleName);
        // Task<IEnumerable<string>> GetUserRolesAsync(string userId);
    }
}
