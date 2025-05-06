using MetalFlowScheduler.Api.Application.Dtos.Auth;
using MetalFlowScheduler.Api.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;

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
        /// Valida as credenciais do usuário e, se válidas, gera um token JWT.
        /// </summary>
        /// <param name="username">Nome de usuário ou email.</param>
        /// <param name="password">Senha.</param>
        /// <returns>O JWT e informações básicas do usuário se a autenticação for bem-sucedida, caso contrário, null.</returns>
        Task<LoginResponseDto?> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Atribui uma role a um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <param name="roleName">O nome da role a ser atribuída.</param>
        /// <returns>Resultado da operação de atribuição de role.</returns>
        Task<IdentityResult> AssignRoleAsync(int userId, string roleName);

        /// <summary>
        /// Adiciona uma claim a um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <param name="claimType">O tipo da claim (ex: "Departamento").</param>
        /// <param name="claimValue">O valor da claim (ex: "Planejamento").</param>
        /// <returns>Resultado da operação de adição de claim.</returns>
        Task<IdentityResult> AddClaimAsync(int userId, string claimType, string claimValue);

        /// <summary>
        /// Atualiza as roles e claims de um usuário em uma única operação.
        /// </summary>
        /// <param name="request">Dados para atualização das permissões (ID do usuário, lista de roles, lista de claims).</param>
        /// <returns>Resultado da operação. Retorna Success se todas as operações Identity forem bem-sucedidas.</returns>
        Task<IdentityResult> UpdateUserPermissionsAsync(UpdateUserPermissionsDto request);

        /// <summary>
        /// Remove uma role específica de um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <param name="roleName">O nome da role a ser removida.</param>
        /// <returns>Resultado da operação de remoção de role.</returns>
        Task<IdentityResult> RemoveRoleAsync(int userId, string roleName);

        /// <summary>
        /// Remove uma claim específica de um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <param name="claimType">O tipo da claim.</param>
        /// <param name="claimValue">O valor da claim.</param>
        /// <returns>Resultado da operação de remoção de claim.</returns>
        Task<IdentityResult> RemoveClaimAsync(int userId, string claimType, string claimValue);

        /// <summary>
        /// Obtém todas as roles atribuídas a um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <returns>Uma coleção de nomes de roles.</returns>
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Obtém todas as claims atribuídas a um usuário.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <returns>Uma lista de Claims.</returns>
        Task<IList<Claim>> GetUserClaimsAsync(int userId);

        /// <summary>
        /// Remove um usuário do sistema.
        /// </summary>
        /// <param name="userId">O ID do usuário a ser removido.</param>
        /// <returns>Resultado da operação de remoção do usuário.</returns>
        Task<IdentityResult> DeleteUserAsync(int userId);

        /// <summary>
        /// Obtém um usuário pelo seu ID.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <returns>A entidade ApplicationUser, ou null se não encontrado.</returns>
        Task<ApplicationUser?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Realiza a operação de logout (principalmente do lado do servidor, se houver revogação).
        /// </summary>
        /// <returns>Uma tarefa completa.</returns>
        Task LogoutAsync(); // Método para logout
    }
}
