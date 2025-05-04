using Asp.Versioning;
using MetalFlowScheduler.Api.Application.Dtos.Auth;
using MetalFlowScheduler.Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization; // Necessário para [AllowAnonymous]
using Microsoft.AspNetCore.Identity; // Necessário para IdentityResult
using Microsoft.Extensions.Logging; // Necessário para ILogger

namespace MetalFlowScheduler.Api.WebApi.Controllers.v1.Auth
{
    /// <summary>
    /// Controlador para autenticação de usuários e geração de tokens JWT.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger; // Adicionado logger

        public AuthController(IAuthService authService, ILogger<AuthController> logger) // Injetar logger
        {
            _authService = authService ?? throw new System.ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger)); // Inicializar logger
        }

        /// <summary>
        /// Autentica um usuário e retorna um token JWT se as credenciais forem válidas.
        /// </summary>
        /// <param name="request">Dados de login (username e senha).</param>
        /// <returns>Token JWT se autenticação bem-sucedida.</returns>
        /// <response code="200">Retorna o token JWT.</response>
        /// <response code="401">Se as credenciais forem inválidas.</response>
        /// <response code="400">Se os dados de entrada forem inválidos.</response>
        [HttpPost("login")]
        [AllowAnonymous] // Permite acesso sem autenticação
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var authResult = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (authResult == null)
            {
                // Log já é feito no serviço, aqui apenas retornamos a resposta HTTP
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
        /// <response code="400">Se os dados de entrada forem inválidos ou o registro falhar (ex: nome de usuário/email já existe).</response>
        [HttpPost("register")]
        [AllowAnonymous] // Geralmente o registro é público, mas pode ser restrito se necessário
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
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
                return Ok("Usuário registrado com sucesso."); // Ou retornar um DTO de sucesso
            }
            else
            {
                // Log os erros do Identity
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Falha no registro do usuário {Username}: {ErrorCode} - {Description}", request.Username, error.Code, error.Description);
                }
                // Retornar erros de validação do Identity
                return BadRequest(result.Errors);
            }
        }

        // Opcional: Adicionar endpoints para gerenciar roles, claims, reset de senha, etc.
        // [HttpPost("assign-role")]
        // [Authorize(Roles = "Admin")] // Exemplo: apenas admins podem atribuir roles
        // public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequestDto request) { ... }
    }
}
