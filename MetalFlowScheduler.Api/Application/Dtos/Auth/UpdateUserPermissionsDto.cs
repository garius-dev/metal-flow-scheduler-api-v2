using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos.Auth
{
    /// <summary>
    /// DTO para a requisição de atualização de roles e claims de um usuário.
    /// </summary>
    public class UpdateUserPermissionsDto
    {
        [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do usuário inválido.")]
        public int UserId { get; set; }

        /// <summary>
        /// Lista de nomes de roles a serem atribuídas/mantidas para o usuário.
        /// Se nulo ou vazio, nenhuma alteração de role será feita (ou todas as roles podem ser removidas, dependendo da lógica do serviço).
        /// Vamos implementar a lógica de adicionar/remover roles para que a lista fornecida seja o estado desejado.
        /// </summary>
        public List<string>? Roles { get; set; }

        /// <summary>
        /// Lista de claims a serem atribuídas/mantidas para o usuário.
        /// Se nulo ou vazio, nenhuma alteração de claim será feita (ou claims de tipos especificados podem ser removidas).
        /// Vamos implementar a lógica de adicionar/remover claims para que a lista fornecida seja o estado desejado para os tipos de claim presentes.
        /// </summary>
        public List<UserClaimDto>? Claims { get; set; }
    }

    /// <summary>
    /// DTO para representar uma claim de usuário (tipo e valor).
    /// </summary>
    public class UserClaimDto
    {
        [Required(ErrorMessage = "O tipo da claim é obrigatório.")]
        [StringLength(100, ErrorMessage = "O tipo da claim não pode exceder 100 caracteres.")]
        public string Type { get; set; }

        [Required(ErrorMessage = "O valor da claim é obrigatório.")]
        [StringLength(256, ErrorMessage = "O valor da claim não pode exceder 256 caracteres.")]
        public string Value { get; set; }
    }
}
