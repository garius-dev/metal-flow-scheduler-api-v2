using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome de usuário não pode exceder 100 caracteres.")]
        public string Username { get; set; } // Pode ser o email

        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        [StringLength(256, ErrorMessage = "O email não pode exceder 256 caracteres.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "A senha e a confirmação de senha não correspondem.")]
        public string ConfirmPassword { get; set; }

        // Opcional: Campo para atribuir uma role inicial no registro
        // [StringLength(50)]
        // public string? Role { get; set; }
    }
}
