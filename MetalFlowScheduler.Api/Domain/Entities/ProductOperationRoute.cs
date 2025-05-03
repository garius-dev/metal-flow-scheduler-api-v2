using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System; // Necessário para DateTime

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Define uma etapa na sequência (rota) de Tipos de Operação necessários
    /// para produzir um determinado Produto, incluindo versionamento.
    /// </summary>
    [Table("ProductsOperationRoutes")]
    public class ProductOperationRoute : BaseEntity
    {
        /// <summary>
        /// Ordem deste tipo de operação na rota do produto.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Número da versão desta definição de rota de produto.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Data/hora de início da validade desta versão da rota.
        /// </summary>
        public DateTime EffectiveStartDate { get; set; }

        /// <summary>
        /// Data/hora de término da validade desta versão da rota (null se for a versão atual).
        /// </summary>
        public DateTime? EffectiveEndDate { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade Product.
        /// </summary>
        [ForeignKey("Product")]
        public int ProductID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Produto ao qual esta rota se refere.
        /// </summary>
        public virtual Product Product { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade OperationType.
        /// </summary>
        [ForeignKey("OperationType")]
        public int OperationTypeID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Tipo de Operação requerido nesta etapa da rota.
        /// </summary>
        public virtual OperationType OperationType { get; set; }
    }
}
