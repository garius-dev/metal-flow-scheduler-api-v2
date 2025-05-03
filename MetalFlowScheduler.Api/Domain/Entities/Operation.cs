using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa uma operação específica que pode ser realizada em um centro de trabalho.
    /// </summary>
    [Table("Operations")]
    public class Operation : BaseEntity
    {
        /// <summary>
        /// Nome da operação.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Tempo de setup (preparação) em minutos necessário para esta operação específica.
        /// </summary>
        public int SetupTimeInMinutes { get; set; }

        /// <summary>
        /// Capacidade de produção desta operação em Toneladas por Hora.
        /// </summary>
        [Required]
        [Column(TypeName = "float")] // double precision no PostgreSQL
        public double Capacity { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade OperationType.
        /// </summary>
        [ForeignKey("OperationType")]
        public int OperationTypeID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Tipo de Operação ao qual esta operação pertence.
        /// </summary>
        public virtual OperationType OperationType { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade WorkCenter.
        /// </summary>
        [ForeignKey("WorkCenter")]
        public int WorkCenterID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Centro de Trabalho onde esta operação é realizada.
        /// </summary>
        public virtual WorkCenter WorkCenter { get; set; }
    }
}
