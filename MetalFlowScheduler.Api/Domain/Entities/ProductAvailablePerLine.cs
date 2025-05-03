using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Entidade de ligação que indica que um determinado Produto pode ser produzido
    /// em uma determinada Linha.
    /// </summary>
    [Table("ProductsAvailablesPerLines")]
    public class ProductAvailablePerLine : BaseEntity
    {
        /// <summary>
        /// Chave estrangeira para a entidade Line.
        /// </summary>
        [ForeignKey("Line")]
        public int LineID { get; set; }
        /// <summary>
        /// Propriedade de navegação para a Linha.
        /// </summary>
        public virtual Line Line { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade Product.
        /// </summary>
        [ForeignKey("Product")]
        public int ProductID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Produto.
        /// </summary>
        public virtual Product Product { get; set; }
    }
}
