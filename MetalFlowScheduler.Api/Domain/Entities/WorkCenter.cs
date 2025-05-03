using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necessário para ICollection

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa um centro de trabalho (máquina, posto de trabalho) dentro de uma linha de produção.
    /// </summary>
    [Table("WorkCenters")]
    public class WorkCenter : BaseEntity
    {
        /// <summary>
        /// Nome do centro de trabalho.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Tamanho ótimo do lote de produção para este centro de trabalho, em Toneladas.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OptimalBatch { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade Line.
        /// </summary>
        [ForeignKey("Line")]
        public int LineID { get; set; }
        /// <summary>
        /// Propriedade de navegação para a Linha à qual este centro de trabalho pertence.
        /// </summary>
        public virtual Line Line { get; set; }

        /// <summary>
        /// Coleção de operações concretas que podem ser realizadas neste centro de trabalho.
        /// </summary>
        public virtual ICollection<Operation> Operations { get; set; } = new List<Operation>();

        /// <summary>
        /// Coleção de etapas de rotas de linha onde este centro de trabalho aparece.
        /// Propriedade de navegação inversa.
        /// </summary>
        public virtual ICollection<LineWorkCenterRoute> LineRoutes { get; set; } = new List<LineWorkCenterRoute>();

        /// <summary>
        /// Coleção de definições de rotas de tipos de operação para este centro de trabalho (com versionamento).
        /// </summary>
        public virtual ICollection<WorkCenterOperationRoute> OperationRoutes { get; set; } = new List<WorkCenterOperationRoute>();

        /// <summary>
        /// Coleção de registros de estoque excedente neste centro de trabalho.
        /// Propriedade de navegação inversa.
        /// </summary>
        public virtual ICollection<SurplusPerProductAndWorkCenter> SurplusStocks { get; set; } = new List<SurplusPerProductAndWorkCenter>();
    }
}
