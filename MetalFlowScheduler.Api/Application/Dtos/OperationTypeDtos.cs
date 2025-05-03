using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos
{
    // DTO para exibir dados de OperationType
    public class OperationTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO para criar um novo OperationType
    public class CreateOperationTypeDto
    {
        [Required(ErrorMessage = "O nome do tipo de operação é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }
    }

    // DTO para atualizar um OperationType existente
    public class UpdateOperationTypeDto
    {
        [Required(ErrorMessage = "O nome do tipo de operação é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }
    }
}
