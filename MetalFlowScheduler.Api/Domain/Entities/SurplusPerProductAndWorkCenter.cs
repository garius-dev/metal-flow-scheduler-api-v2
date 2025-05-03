using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Registra a quantidade de estoque excedente (surplus) de um Produto
    /// específico em um determinado Centro de Trabalho.
    /// </summary>
    [Table("SurplusPerProductAndWorkCenter")]
    public class SurplusPerProductAndWorkCenter : BaseEntity
    {
        /// <summary>
        /// Chave estrangeira para a entidade Product.
        /// </summary>
        [ForeignKey("Product")]
        public int ProductID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Produto.
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade WorkCenter.
        /// </summary>
        [ForeignKey("WorkCenter")]
        public int WorkCenterID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Centro de Trabalho.
        /// </summary>
        public virtual WorkCenter WorkCenter { get; set; }

        /// <summary>
        /// Quantidade excedente do produto neste centro de trabalho, em Toneladas.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Surplus { get; set; }
    }
}
