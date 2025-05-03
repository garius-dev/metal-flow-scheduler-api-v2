using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos
{
    // DTO para exibir dados de Product
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal UnitPricePerTon { get; set; }
        public decimal ProfitMargin { get; set; }
        public int Priority { get; set; }
        public decimal PenaltyCost { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO para criar um novo Product
    public class CreateProductDto
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O preço unitário por tonelada é obrigatório.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O preço deve ser um valor positivo.")]
        public decimal UnitPricePerTon { get; set; }

        [Required(ErrorMessage = "A margem de lucro é obrigatória.")]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "A margem de lucro não pode ser negativa.")]
        public decimal ProfitMargin { get; set; }

        [Required(ErrorMessage = "A prioridade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A prioridade deve ser um valor positivo.")]
        public int Priority { get; set; }

        [Required(ErrorMessage = "O custo de penalidade é obrigatório.")]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "O custo de penalidade não pode ser negativo.")]
        public decimal PenaltyCost { get; set; }

        // C07: Receber lista OPCIONAL de IDs de OperationType para criar ProductOperationRoute
        public List<int>? OperationTypeIds { get; set; }
    }

    // DTO para atualizar um Product existente
    public class UpdateProductDto
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O preço unitário por tonelada é obrigatório.")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O preço deve ser um valor positivo.")]
        public decimal UnitPricePerTon { get; set; }

        [Required(ErrorMessage = "A margem de lucro é obrigatória.")]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "A margem de lucro não pode ser negativa.")]
        public decimal ProfitMargin { get; set; }

        [Required(ErrorMessage = "A prioridade é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A prioridade deve ser um valor positivo.")]
        public int Priority { get; set; }

        [Required(ErrorMessage = "O custo de penalidade é obrigatório.")]
        [Range(typeof(decimal), "0.00", "79228162514264337593543950335", ErrorMessage = "O custo de penalidade não pode ser negativo.")]
        public decimal PenaltyCost { get; set; }

        // C09: Receber lista OPCIONAL de IDs de OperationType para atualizar ProductOperationRoute
        public List<int>? OperationTypeIds { get; set; }
    }
}
