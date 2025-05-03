using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necessário para ICollection

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa um produto a ser fabricado.
    /// </summary>
    [Table("Products")]
    public class Product : BaseEntity
    {
        /// <summary>
        /// Nome do produto. Deve ser único.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Preço unitário de venda por tonelada do produto.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPricePerTon { get; set; }

        /// <summary>
        /// Margem de lucro percentual do produto.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 4)")] // Precisão maior para percentual
        public decimal ProfitMargin { get; set; }

        /// <summary>
        /// Nível de prioridade do produto para agendamento.
        /// </summary>
        [Required]
        public int Priority { get; set; }

        /// <summary>
        /// Custo de penalidade por unidade de tempo ou produto em caso de atraso.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PenaltyCost { get; set; } // Corrigido de PenalityCost

        /// <summary>
        /// Coleção de etapas da rota de tipos de operação necessárias para produzir este produto (com versionamento).
        /// </summary>
        public virtual ICollection<ProductOperationRoute> OperationRoutes { get; set; } = new List<ProductOperationRoute>();

        /// <summary>
        /// Coleção de associações que indicam em quais linhas este produto pode ser produzido.
        /// (Tabela de ligação).
        /// </summary>
        public virtual ICollection<ProductAvailablePerLine> AvailableOnLines { get; set; } = new List<ProductAvailablePerLine>();

        /// <summary>
        /// Coleção de itens de ordens de produção que se referem a este produto.
        /// Propriedade de navegação inversa.
        /// </summary>
        public virtual ICollection<ProductionOrderItem> ProductionOrderItems { get; set; } = new List<ProductionOrderItem>();

        /// <summary>
        /// Coleção de registros de estoque excedente deste produto em diferentes centros de trabalho.
        /// Propriedade de navegação inversa.
        /// </summary>
        public virtual ICollection<SurplusPerProductAndWorkCenter> SurplusStocks { get; set; } = new List<SurplusPerProductAndWorkCenter>();
    }
}
