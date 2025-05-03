using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa um item dentro de uma Ordem de Produção, especificando
    /// o Produto e a Quantidade a ser produzida.
    /// </summary>
    [Table("ProductionOrderItems")]
    public class ProductionOrderItem : BaseEntity
    {
        /// <summary>
        /// Chave estrangeira para a entidade ProductionOrder pai.
        /// </summary>
        [ForeignKey("ProductionOrder")]
        public int ProductionOrderID { get; set; }
        /// <summary>
        /// Propriedade de navegação para a Ordem de Produção à qual este item pertence.
        /// </summary>
        public virtual ProductionOrder ProductionOrder { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade Product.
        /// </summary>
        [ForeignKey("Product")]
        public int ProductID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Produto a ser produzido.
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Quantidade deste produto a ser produzida, em Toneladas.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Quantity { get; set; }
    }
}
