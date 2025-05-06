using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos.Auth;
using MetalFlowScheduler.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1.Auth
{
    /// <summary>
    /// Controlador para autenticação de usuários e gerenciamento básico de usuários/roles/claims.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Autentica um usuário e retorna um token JWT se as credenciais forem válidas.
        /// </summary>
        /// <param name="request">Dados de login (username e senha).</param>
        /// <returns>Token JWT se autenticação bem-sucedida.</returns>
        /// <response code="200">Retorna o token JWT.</response>
        /// <response code="401">Se as credenciais forem inválidas (tratado pelo serviço).</response>
        /// <response code="400">Se os dados de entrada forem inválidos (validação do DTO).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authResult = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (authResult == null)
            {
                return Unauthorized("Nome de usuário ou senha inválidos.");
            }

            return Ok(authResult);
        }

        /// <summary>
        /// Registra um novo usuário no sistema.
        /// </summary>
        /// <param name="request">Dados para criação do usuário (username, email, senha).</param>
        /// <returns>Resultado da operação de registro.</returns>
        /// <response code="200">Usuário registrado com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou o registro falhar (erros do Identity).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);

            if (result.Succeeded)
            {
                _logger.LogInformation("Novo usuário registrado: {Username}", request.Username);
                return Ok("Usuário registrado com sucesso.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Atribui uma role a um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para atribuição de role (ID do usuário e nome da role).</param>
        /// <returns>Resultado da operação de atribuição de role.</returns>
        /// <response code="200">Role atribuída com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a atribuição falhar (usuário/role não encontrado, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para atribuir roles (não atende à política/role).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("assign-role")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATRIBUIR roles
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.AssignRoleAsync(request.UserId, request.RoleName);

            if (result.Succeeded)
            {
                return Ok($"Role '{request.RoleName}' atribuída ao usuário ID {request.UserId}.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Adiciona uma claim a um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para adição de claim (ID do usuário, tipo e valor da claim).</param>
        /// <returns>Resultado da operação de adição de claim.</returns>
        /// <response code="200">Claim adicionada com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a adição falhar (usuário não encontrado, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para adicionar claims (não atende à política/role).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("add-claim")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATRIBUIR claims
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddClaim([FromBody] AddClaimRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.AddClaimAsync(request.UserId, request.ClaimType, request.ClaimValue);

            if (result.Succeeded)
            {
                return Ok($"Claim '{request.ClaimType}:{request.ClaimValue}' adicionada ao usuário ID {request.UserId}.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Atualiza as roles e claims de um usuário em uma única operação. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para atualização das permissões (ID do usuário, lista de roles, lista de claims).</param>
        /// <returns>Resultado da operação.</returns>
        /// <response code="200">Permissões atualizadas com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a atualização falhar (usuário/role não encontrado, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para atualizar permissões.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("update-permissions")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATUALIZAR permissões
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateUserPermissions([FromBody] UpdateUserPermissionsDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.UpdateUserPermissionsAsync(request);

            if (result.Succeeded)
            {
                return Ok($"Permissões (roles e claims) atualizadas com sucesso para o usuário ID {request.UserId}.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Remove uma role de um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para remoção de role (ID do usuário e nome da role).</param>
        /// <returns>Resultado da operação de remoção de role.</returns>
        /// <response code="200">Role removida com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a remoção falhar (usuário não encontrado, role não encontrada para o usuário, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover roles.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("remove-role")] // Usando DELETE para remoção
        [Authorize(Policy = "CanRemoveRoles")] // ** Política atualizada **
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequestDto request) // Reutilizando DTO
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RemoveRoleAsync(request.UserId, request.RoleName);

            if (result.Succeeded)
            {
                return Ok($"Role '{request.RoleName}' removida do usuário ID {request.UserId}.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Remove uma claim de um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para remoção de claim (ID do usuário, tipo e valor da claim).</param>
        /// <returns>Resultado da operação de remoção de claim.</returns>
        /// <response code="200">Claim removida com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a remoção falhar (usuário não encontrado, claim não encontrada para o usuário, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover claims.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("remove-claim")] // Usando DELETE para remoção
        [Authorize(Policy = "CanRemoveClaims")] // ** Política atualizada **
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveClaim([FromBody] AddClaimRequestDto request) // Reutilizando DTO
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RemoveClaimAsync(request.UserId, request.ClaimType, request.ClaimValue);

            if (result.Succeeded)
            {
                return Ok($"Claim '{request.ClaimType}:{request.ClaimValue}' removida do usuário ID {request.UserId}.");
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Obtém todas as roles de um usuário específico. Requer permissões elevadas.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <returns>Uma lista de nomes de roles.</returns>
        /// <response code="200">Retorna a lista de roles.</response>
        /// <response code="400">Se o ID do usuário for inválido.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para listar roles.</response>
        /// <response code="404">Se o usuário não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("user-roles/{userId}")] // Usando GET com ID na rota
        [Authorize(Policy = "CanAssignRoles")] // Use uma política apropriada (pode ser menos restritiva)
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<string>>> GetUserRoles(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("ID de usuário inválido.");
            }

            var roles = await _authService.GetUserRolesAsync(userId);

            return Ok(roles);
        }

        /// <summary>
        /// Obtém todas as claims de um usuário específico. Requer permissões elevadas.
        /// </summary>
        /// <param name="userId">O ID do usuário.</param>
        /// <returns>Uma lista de Claims.</returns>
        /// <response code="200">Retorna a lista de claims.</response>
        /// <response code="400">Se o ID do usuário for inválido.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para listar claims.</response>
        /// <response code="404">Se o usuário não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("user-claims/{userId}")] // Usando GET com ID na rota
        [Authorize(Policy = "CanAssignRoles")] // Use uma política apropriada (pode ser menos restritiva)
        [ProducesResponseType(typeof(IList<Claim>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IList<Claim>>> GetUserClaims(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("ID de usuário inválido.");
            }

            var claims = await _authService.GetUserClaimsAsync(userId);

            return Ok(claims);
        }

        /// <summary>
        /// Obtém as roles do usuário autenticado.
        /// </summary>
        /// <returns>Uma lista de nomes de roles.</returns>
        /// <response code="200">Retorna a lista de roles do usuário autenticado.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("my-roles")]
        [Authorize] // Exige apenas que o usuário esteja autenticado
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<string>>> GetMyRoles()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogError("Claim {ClaimType} não encontrada ou inválida para usuário autenticado.", ClaimTypes.NameIdentifier);
                return Unauthorized();
            }

            var roles = await _authService.GetUserRolesAsync(userId);

            return Ok(roles);
        }

        /// <summary>
        /// Obtém as claims do usuário autenticado.
        /// </summary>
        /// <returns>Uma lista de Claims.</returns>
        /// <response code="200">Retorna a lista de claims do usuário autenticado.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("my-claims")]
        [Authorize] // Exige apenas que o usuário esteja autenticado
        [ProducesResponseType(typeof(IList<Claim>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IList<Claim>>> GetMyClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                _logger.LogError("Claim {ClaimType} não encontrada ou inválida para usuário autenticado.", ClaimTypes.NameIdentifier);
                return Unauthorized();
            }

            var claims = await _authService.GetUserClaimsAsync(userId);

            return Ok(claims);
        }

        /// <summary>
        /// Remove um usuário do sistema pelo seu ID. Requer permissões elevadas.
        /// </summary>
        /// <param name="userId">O ID do usuário a ser removido.</param>
        /// <returns>Resultado da operação de remoção do usuário.</returns>
        /// <response code="200">Usuário removido com sucesso.</response>
        /// <response code="400">Se o ID do usuário for inválido ou a remoção falhar (usuário não encontrado, erros do Identity).</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover usuários.</response>
        /// <response code="404">Se o usuário não for encontrado (tratado pelo serviço/middleware).</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("users/{userId}")] // Usando DELETE com ID na rota
        [Authorize(Policy = "CanDeleteUsers")] // ** Nova Política **
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("ID de usuário inválido.");
            }

            var result = await _authService.DeleteUserAsync(userId);

            if (result.Succeeded)
            {
                return Ok($"Usuário com ID {userId} removido com sucesso.");
            }
            else
            {
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    _logger.LogWarning("Tentativa de remover usuário não encontrado com ID {UserId}.", userId);
                    return NotFound(result.Errors);
                }
                return BadRequest(result.Errors);
            }
        }
    }
}
