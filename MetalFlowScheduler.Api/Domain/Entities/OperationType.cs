using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Necessário para ICollection

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa um tipo ou categoria de operação (ex: Corte, Dobra, Solda).
    /// </summary>
    [Table("OperationTypes")]
    public class OperationType : BaseEntity
    {
        /// <summary>
        /// Nome do tipo de operação. Deve ser único.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Coleção de operações concretas que pertencem a este tipo.
        /// </summary>
        public virtual ICollection<Operation> Operations { get; set; } = new List<Operation>();

        /// <summary>
        /// Coleção de etapas de rotas de centro de trabalho onde este tipo de operação aparece.
        /// </summary>
        public virtual ICollection<WorkCenterOperationRoute> WorkCenterRoutes { get; set; } = new List<WorkCenterOperationRoute>();

        /// <summary>
        /// Coleção de etapas de rotas de produto onde este tipo de operação é requerido.
        /// </summary>
        public virtual ICollection<ProductOperationRoute> ProductRoutes { get; set; } = new List<ProductOperationRoute>();
    }
}
