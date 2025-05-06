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
        /// <response code="401">Se as credenciais forem inválidas.</response>
        /// <response code="400">Se os dados de entrada forem inválidos.</response>
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
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var authResult = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (authResult == null)
            {
                // 401 Unauthorized - Credenciais inválidas (tratado no serviço retornando null)
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
        /// <response code="400">Se os dados de entrada forem inválidos ou o registro falhar.</response>
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
                // 400 Bad Request com detalhes da validação do DTO
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
                // 400 Bad Request com erros do Identity (nome/email duplicado, senha fraca, etc.)
                // Os erros do IdentityResult já contêm descrições amigáveis.
                return BadRequest(result.Errors);
            }
        }

        /// <summary>
        /// Atribui uma role a um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para atribuição de role (ID do usuário e nome da role).</param>
        /// <returns>Resultado da operação de atribuição de role.</returns>
        /// <response code="200">Role atribuída com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a atribuição falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para atribuir roles.</response>
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("assign-role")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATRIBUIR roles
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var result = await _authService.AssignRoleAsync(request.UserId, request.RoleName);

            if (result.Succeeded)
            {
                return Ok($"Role '{request.RoleName}' atribuída ao usuário ID {request.UserId}.");
            }
            else
            {
                // Inspeciona os erros do IdentityResult para retornar códigos mais específicos
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    // 404 Not Found - Usuário não encontrado
                    _logger.LogWarning("Falha na atribuição de role: Usuário com ID {UserId} não encontrado.", request.UserId);
                    return NotFound($"Usuário com ID {request.UserId} não encontrado.");
                }
                if (result.Errors.Any(e => e.Code == "RoleNotFound"))
                {
                    // 400 Bad Request - Role não encontrada no sistema
                    _logger.LogWarning("Falha na atribuição de role: Role '{RoleName}' não encontrada.", request.RoleName);
                    return BadRequest($"Role '{request.RoleName}' não encontrada.");
                }
                // Outros erros do Identity (ex: usuário já tem a role, etc.) retornam 400
                _logger.LogWarning("Falha na atribuição de role para usuário ID {UserId}. Erros: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
            }
        }

        /// <summary>
        /// Adiciona uma claim a um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para adição de claim (ID do usuário, tipo e valor da claim).</param>
        /// <returns>Resultado da operação de adição de claim.</returns>
        /// <response code="200">Claim adicionada com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a adição falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para adicionar claims.</response>
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("add-claim")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATRIBUIR claims
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> AddClaim([FromBody] AddClaimRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var result = await _authService.AddClaimAsync(request.UserId, request.ClaimType, request.ClaimValue);

            if (result.Succeeded)
            {
                return Ok($"Claim '{request.ClaimType}:{request.ClaimValue}' adicionada ao usuário ID {request.UserId}.");
            }
            else
            {
                // Inspeciona os erros do IdentityResult
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    // 404 Not Found - Usuário não encontrado
                    _logger.LogWarning("Falha na adição de claim: Usuário com ID {UserId} não encontrado.", request.UserId);
                    return NotFound($"Usuário com ID {request.UserId} não encontrado.");
                }
                // Outros erros do Identity retornam 400
                _logger.LogWarning("Falha na adição de claim para usuário ID {UserId}. Erros: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
            }
        }

        /// <summary>
        /// Atualiza as roles e claims de um usuário em uma única operação. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para atualização das permissões (ID do usuário, lista de roles, lista de claims).</param>
        /// <returns>Resultado da operação.</returns>
        /// <response code="200">Permissões atualizadas com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a atualização falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para atualizar permissões.</response>
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("update-permissions")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode ATUALIZAR permissões
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateUserPermissions([FromBody] UpdateUserPermissionsDto request)
        {
            if (!ModelState.IsValid)
            {
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var result = await _authService.UpdateUserPermissionsAsync(request);

            if (result.Succeeded)
            {
                return Ok($"Permissões (roles e claims) atualizadas com sucesso para o usuário ID {request.UserId}.");
            }
            else
            {
                // Inspeciona os erros do IdentityResult
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    // 404 Not Found - Usuário não encontrado
                    _logger.LogWarning("Falha na atualização de permissões: Usuário com ID {UserId} não encontrado.", request.UserId);
                    return NotFound($"Usuário com ID {request.UserId} não encontrado.");
                }
                if (result.Errors.Any(e => e.Code == "RoleNotFound"))
                {
                    // 400 Bad Request - Alguma role na lista não foi encontrada
                    _logger.LogWarning("Falha na atualização de permissões: Alguma role não encontrada.");
                    return BadRequest(result.Errors); // Retorna os erros do IdentityResult, que inclui qual role não foi encontrada
                }
                if (result.Errors.Any(e => e.Code == "PermissionDenied"))
                {
                    // 403 Forbidden - Lógica de restrição no serviço negou a operação
                    _logger.LogWarning("Falha na atualização de permissões: Permissão negada pelo serviço para usuário ID {UserId}.", request.UserId);
                    return StatusCode(403, "Você não tem permissão para atualizar as permissões deste usuário.");
                    // Ou retornar os erros do IdentityResult que contêm a descrição do motivo da negação
                    // return StatusCode(403, result.Errors);
                }

                // Outros erros do Identity retornam 400
                _logger.LogWarning("Falha na atualização de permissões para usuário ID {UserId}. Erros: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
            }
        }

        /// <summary>
        /// Remove uma role de um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para remoção de role (ID do usuário e nome da role).</param>
        /// <returns>Resultado da operação de remoção de role.</returns>
        /// <response code="200">Role removida com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a remoção falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover roles.</response>
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("remove-role")] // Usando DELETE para remoção
        [Authorize(Policy = "CanRemoveRoles")] // Política para quem pode REMOVER roles
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequestDto request) // Reutilizando DTO
        {
            if (!ModelState.IsValid)
            {
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var result = await _authService.RemoveRoleAsync(request.UserId, request.RoleName);

            if (result.Succeeded)
            {
                return Ok($"Role '{request.RoleName}' removida do usuário ID {request.UserId}.");
            }
            else
            {
                // Inspeciona os erros do IdentityResult
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    // 404 Not Found - Usuário não encontrado
                    _logger.LogWarning("Falha na remoção de role: Usuário com ID {UserId} não encontrado.", request.UserId);
                    return NotFound($"Usuário com ID {request.UserId} não encontrado.");
                }
                if (result.Errors.Any(e => e.Code == "PermissionDenied"))
                {
                    // 403 Forbidden - Lógica de restrição no serviço negou a operação
                    _logger.LogWarning("Falha na remoção de role: Permissão negada pelo serviço para usuário ID {UserId}.", request.UserId);
                    return StatusCode(403, "Você não tem permissão para remover esta role deste usuário.");
                    // Ou retornar os erros do IdentityResult que contêm a descrição do motivo da negação
                    // return StatusCode(403, result.Errors);
                }
                // Outros erros do Identity retornam 400
                _logger.LogWarning("Falha na remoção de role para usuário ID {UserId}. Erros: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
            }
        }

        /// <summary>
        /// Remove uma claim de um usuário existente. Requer permissões elevadas.
        /// </summary>
        /// <param name="request">Dados para remoção de claim (ID do usuário, tipo e valor da claim).</param>
        /// <returns>Resultado da operação de remoção de claim.</returns>
        /// <response code="200">Claim removida com sucesso.</response>
        /// <response code="400">Se os dados de entrada forem inválidos ou a remoção falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover claims.</response>
        /// <response code="404">Se o usuário não for encontrado ou a claim não for encontrada para o usuário.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("remove-claim")] // Usando DELETE para remoção
        [Authorize(Policy = "CanRemoveClaims")] // Política para quem pode REMOVER claims
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> RemoveClaim([FromBody] AddClaimRequestDto request) // Reutilizando DTO
        {
            if (!ModelState.IsValid)
            {
                // 400 Bad Request com detalhes da validação do DTO
                return BadRequest(ModelState);
            }

            var result = await _authService.RemoveClaimAsync(request.UserId, request.ClaimType, request.ClaimValue);

            if (result.Succeeded)
            {
                return Ok($"Claim '{request.ClaimType}:{request.ClaimValue}' removida do usuário ID {request.UserId}.");
            }
            else
            {
                // Inspeciona os erros do IdentityResult
                if (result.Errors.Any(e => e.Code == "UserNotFound") || result.Errors.Any(e => e.Code == "ClaimNotFound"))
                {
                    // 404 Not Found - Usuário ou Claim específica não encontrada
                    _logger.LogWarning("Falha na remoção de claim: Usuário com ID {UserId} ou Claim '{ClaimType}:{ClaimValue}' não encontrados.", request.UserId, request.ClaimType, request.ClaimValue);
                    // Retorna os erros do IdentityResult, que inclui qual não foi encontrado
                    return NotFound(result.Errors);
                }
                if (result.Errors.Any(e => e.Code == "PermissionDenied"))
                {
                    // 403 Forbidden - Lógica de restrição no serviço negou a operação
                    _logger.LogWarning("Falha na remoção de claim: Permissão negada pelo serviço para usuário ID {UserId}.", request.UserId);
                    return StatusCode(403, "Você não tem permissão para remover claims deste usuário.");
                    // Ou retornar os erros do IdentityResult que contêm a descrição do motivo da negação
                    // return StatusCode(403, result.Errors);
                }
                // Outros erros do Identity retornam 400
                _logger.LogWarning("Falha na remoção de claim para usuário ID {UserId}. Erros: {Errors}", request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
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
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("user-roles/{userId}")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode VER roles de outros usuários
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

            // Usando o novo método GetUserByIdAsync para verificar a existência antes de obter roles
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                // 404 Not Found - Usuário não encontrado
                _logger.LogWarning("Tentativa de obter roles de usuário não encontrado com ID {UserId}.", userId);
                return NotFound($"Usuário com ID {userId} não encontrado.");
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
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpGet("user-claims/{userId}")]
        [Authorize(Policy = "CanAssignRoles")] // Política para quem pode VER claims de outros usuários
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

            // Usando o novo método GetUserByIdAsync para verificar a existência antes de obter claims
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                // 404 Not Found - Usuário não encontrado
                _logger.LogWarning("Tentativa de obter claims de usuário não encontrado com ID {UserId}.", userId);
                return NotFound($"Usuário com ID {userId} não encontrado.");
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
        /// <response code="400">Se o ID do usuário for inválido ou a remoção falhar.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não tiver permissão para remover usuários ou se a lógica de restrição de usuário alvo negar a operação.</response>
        /// <response code="404">Se o usuário não for encontrado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpDelete("users/{userId}")] // Usando DELETE com ID na rota
        [Authorize(Policy = "CanDeleteUsers")] // Política para quem pode DELETAR usuários
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
                // Inspeciona os erros do IdentityResult
                if (result.Errors.Any(e => e.Code == "UserNotFound"))
                {
                    // 404 Not Found - Usuário não encontrado
                    _logger.LogWarning("Falha na remoção de usuário: Usuário com ID {UserId} não encontrado.", userId);
                    return NotFound($"Usuário com ID {userId} não encontrado.");
                }
                if (result.Errors.Any(e => e.Code == "PermissionDenied"))
                {
                    // 403 Forbidden - Lógica de restrição no serviço negou a operação
                    _logger.LogWarning("Falha na remoção de usuário: Permissão negada pelo serviço para usuário ID {UserId}.", userId);
                    return StatusCode(403, "Você não tem permissão para remover este usuário.");
                    // Ou retornar os erros do IdentityResult que contêm a descrição do motivo da negação
                    // return StatusCode(403, result.Errors);
                }
                // Outros erros do Identity retornam 400
                _logger.LogWarning("Falha na remoção de usuário com ID {UserId}. Erros: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors); // Retorna os erros do IdentityResult
            }
        }

        /// <summary>
        /// Realiza a operação de logout (principalmente do lado do servidor, se houver revogação).
        /// </summary>
        /// <returns>Uma resposta de sucesso.</returns>
        /// <response code="200">Logout bem-sucedido.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="500">Se ocorrer um erro inesperado no servidor (tratado globalmente).</response>
        [HttpPost("logout")] // Usando POST para logout
        [Authorize] // Exige que o usuário esteja autenticado para fazer logout
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Logout()
        {
            // Em uma API JWT stateless, a ação de logout é primariamente do lado do cliente
            // (descartar o token). Este endpoint pode ser usado para:
            // 1. Sinalizar ao servidor que o token não será mais usado.
            // 2. Implementar lógica de revogação de token (adicionar JTI a uma lista negra).
            // 3. Limpar cookies ou outras informações de sessão (se aplicável, mas menos comum com JWT puro).

            // Chama o serviço de autenticação. Em uma implementação básica, ele não faz nada.
            // Se você tiver lógica de revogação no serviço, ela será executada aqui.
            await _authService.LogoutAsync();

            _logger.LogInformation("Usuário autenticado solicitou logout.");

            // Retorna 200 OK para indicar que a requisição foi processada.
            // A ação real de "deslogar" acontece quando o cliente descarta o token.
            return Ok("Logout bem-sucedido.");
        }
    }
}
