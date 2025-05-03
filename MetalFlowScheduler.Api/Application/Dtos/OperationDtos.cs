using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos
{
    // DTO para exibir dados de Operation
    public class OperationDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int SetupTimeInMinutes { get; set; }
        public double Capacity { get; set; } // Toneladas por Hora
        public int OperationTypeId { get; set; }
        public string? OperationTypeName { get; set; }
        public int WorkCenterId { get; set; }
        public string? WorkCenterName { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO para criar uma nova Operation
    public class CreateOperationDto
    {
        [Required(ErrorMessage = "O nome da operação é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "O tempo de setup deve ser um valor não negativo.")]
        public int SetupTimeInMinutes { get; set; }

        [Required(ErrorMessage = "A capacidade é obrigatória.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "A capacidade deve ser um valor positivo.")]
        public double Capacity { get; set; }

        [Required(ErrorMessage = "O ID do tipo de operação é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do tipo de operação inválido.")]
        public int OperationTypeId { get; set; }

        [Required(ErrorMessage = "O ID do centro de trabalho é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do centro de trabalho inválido.")]
        public int WorkCenterId { get; set; }
    }

    // DTO para atualizar uma Operation existente
    public class UpdateOperationDto
    {
        [Required(ErrorMessage = "O nome da operação é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "O tempo de setup deve ser um valor não negativo.")]
        public int SetupTimeInMinutes { get; set; }

        [Required(ErrorMessage = "A capacidade é obrigatória.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "A capacidade deve ser um valor positivo.")]
        public double Capacity { get; set; }

        [Required(ErrorMessage = "O ID do tipo de operação é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do tipo de operação inválido.")]
        public int OperationTypeId { get; set; }

        [Required(ErrorMessage = "O ID do centro de trabalho é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do centro de trabalho inválido.")]
        public int WorkCenterId { get; set; }
    }
}
