using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necessário para ICollection

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa uma linha de produção na fábrica.
    /// </summary>
    [Table("Lines")]
    public class Line : BaseEntity
    {
        /// <summary>
        /// Nome da linha de produção. Deve ser único.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Coleção de centros de trabalho que pertencem a esta linha.
        /// Propriedade de navegação inversa.
        /// </summary>
        public virtual ICollection<WorkCenter> WorkCenters { get; set; } = new List<WorkCenter>();

        /// <summary>
        /// Coleção de definições de rotas de centros de trabalho para esta linha (com versionamento).
        /// Propriedade de navegação.
        /// </summary>
        public virtual ICollection<LineWorkCenterRoute> WorkCenterRoutes { get; set; } = new List<LineWorkCenterRoute>();

        /// <summary>
        /// Coleção de associações que indicam quais produtos podem ser produzidos nesta linha.
        /// Propriedade de navegação (tabela de ligação).
        /// </summary>
        public virtual ICollection<ProductAvailablePerLine> AvailableProducts { get; set; } = new List<ProductAvailablePerLine>();
    }
}
