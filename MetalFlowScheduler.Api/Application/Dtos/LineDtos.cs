using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Application.Dtos
{
    // DTO para exibir dados de Line
    public class LineDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    // DTO para criar uma nova Line
    public class CreateLineDto
    {
        [Required(ErrorMessage = "O nome da linha é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        // C07: Receber lista de IDs de WorkCenter para criar LineWorkCenterRoute
        [Required(ErrorMessage = "É necessário fornecer os IDs dos centros de trabalho.")]
        [MinLength(1, ErrorMessage = "É necessário fornecer pelo menos um ID de centro de trabalho.")]
        public List<int> WorkCenterIds { get; set; } = new();

        // C07: Receber lista OPCIONAL de IDs de Product para criar ProductAvailablePerLine
        public List<int>? ProductIds { get; set; }
    }

    // DTO para atualizar uma Line existente
    public class UpdateLineDto
    {
        [Required(ErrorMessage = "O nome da linha é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; }

        // C09: Receber lista de IDs de WorkCenter para atualizar LineWorkCenterRoute
        [Required(ErrorMessage = "É necessário fornecer os IDs dos centros de trabalho.")]
        [MinLength(1, ErrorMessage = "É necessário fornecer pelo menos um ID de centro de trabalho.")]
        public List<int> WorkCenterIds { get; set; } = new();

        // C09: Receber lista OPCIONAL de IDs de Product para atualizar ProductAvailablePerLine
        public List<int>? ProductIds { get; set; }
    }
}
