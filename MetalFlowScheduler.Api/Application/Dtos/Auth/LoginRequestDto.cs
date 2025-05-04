using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    /// <summary>
    /// DTO para a requisição de login.
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        public string Username { get; set; } // Pode ser o email, dependendo de como você configura o Identity

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
