using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos
{
    // DTO para exibir dados de WorkCenter
    public class WorkCenterDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal OptimalBatch { get; set; } // Toneladas
        public int LineId { get; set; }
        public string? LineName { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO para criar um novo WorkCenter
    public class CreateWorkCenterDto
    {
        [Required(ErrorMessage = "O nome do centro de trabalho é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O lote ótimo é obrigatório.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O lote ótimo deve ser um valor positivo.")]
        public decimal OptimalBatch { get; set; }

        [Required(ErrorMessage = "O ID da linha é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID da linha inválido.")]
        public int LineId { get; set; }

        // C07: Receber lista de IDs de OperationType para criar WorkCenterOperationRoute
        [Required(ErrorMessage = "É necessário fornecer os IDs dos tipos de operação.")]
        [MinLength(1, ErrorMessage = "É necessário fornecer pelo menos um ID de tipo de operação.")]
        public List<int> OperationTypeIds { get; set; } = new();
    }

    // DTO para atualizar um WorkCenter existente
    public class UpdateWorkCenterDto
    {
        [Required(ErrorMessage = "O nome do centro de trabalho é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O lote ótimo é obrigatório.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O lote ótimo deve ser um valor positivo.")]
        public decimal OptimalBatch { get; set; }

        [Required(ErrorMessage = "O ID da linha é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID da linha inválido.")]
        public int LineId { get; set; }

        // C09: Receber lista de IDs de OperationType para atualizar WorkCenterOperationRoute
        [Required(ErrorMessage = "É necessário fornecer os IDs dos tipos de operação.")]
        [MinLength(1, ErrorMessage = "É necessário fornecer pelo menos um ID de tipo de operação.")]
        public List<int> OperationTypeIds { get; set; } = new();
    }
}
