using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    public class AssignRoleRequestDto
    {
        [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do usuário inválido.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "O nome da role é obrigatório.")]
        [StringLength(50, ErrorMessage = "O nome da role não pode exceder 50 caracteres.")] // Ajuste o tamanho conforme necessário
        public string RoleName { get; set; }
    }
}
