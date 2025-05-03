using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System; // Necessário para DateTime

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Define uma etapa na sequência (rota) de Tipos de Operação DENTRO
    /// de um Centro de Trabalho específico, incluindo versionamento e tempo de transporte interno.
    /// </summary>
    [Table("WorkCentersOperationRoutes")]
    public class WorkCenterOperationRoute : BaseEntity
    {
        /// <summary>
        /// Nome ou identificador opcional para esta etapa da rota interna do centro de trabalho.
        /// </summary>
        [StringLength(50)]
        public string? Name { get; set; } // Permitido ser nulo

        /// <summary>
        /// Ordem desta etapa na rota interna do centro de trabalho.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Número da versão desta definição de rota interna.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Data/hora de início da validade desta versão da rota interna.
        /// </summary>
        public DateTime EffectiveStartDate { get; set; }

        /// <summary>
        /// Data/hora de término da validade desta versão da rota interna (null se for a versão atual).
        /// </summary>
        public DateTime? EffectiveEndDate { get; set; }

        /// <summary>
        /// Tempo de transporte em minutos APÓS completar este tipo de operação
        /// para o próximo tipo de operação DENTRO do mesmo Centro de Trabalho.
        /// </summary>
        [Required]
        public int TransportTimeInMinutes { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade WorkCenter.
        /// </summary>
        [ForeignKey("WorkCenter")]
        public int WorkCenterID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Centro de Trabalho ao qual esta rota interna pertence.
        /// </summary>
        public virtual WorkCenter WorkCenter { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade OperationType.
        /// </summary>
        [ForeignKey("OperationType")]
        public int OperationTypeID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Tipo de Operação referenciado nesta etapa da rota interna.
        /// </summary>
        public virtual OperationType OperationType { get; set; }
    }
}
