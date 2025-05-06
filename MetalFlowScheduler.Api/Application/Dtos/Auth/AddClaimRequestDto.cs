using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    public class AddClaimRequestDto
    {
        [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do usuário inválido.")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "O tipo da claim é obrigatório.")]
        [StringLength(100, ErrorMessage = "O tipo da claim não pode exceder 100 caracteres.")] // Ajuste o tamanho conforme necessário
        public string ClaimType { get; set; }

        [Required(ErrorMessage = "O valor da claim é obrigatório.")]
        [StringLength(256, ErrorMessage = "O valor da claim não pode exceder 256 caracteres.")] // Ajuste o tamanho conforme necessário
        public string ClaimValue { get; set; }
    }
}
