using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System; // Necessário para DateTime

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Define um passo na sequência (rota) de centros de trabalho dentro de uma linha de produção,
    /// incluindo versionamento e tempo de transporte.
    /// </summary>
    [Table("LinesWorkCentersRoutes")]
    public class LineWorkCenterRoute : BaseEntity
    {
        /// <summary>
        /// Ordem deste centro de trabalho na rota da linha.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Número da versão desta definição de rota.
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
        /// Tempo de transporte em minutos APÓS completar este centro de trabalho
        /// para o próximo centro de trabalho na sequência da linha.
        /// </summary>
        [Required]
        public int TransportTimeInMinutes { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade Line.
        /// </summary>
        [ForeignKey("Line")]
        public int LineID { get; set; }
        /// <summary>
        /// Propriedade de navegação para a Linha à qual esta rota pertence.
        /// </summary>
        public virtual Line Line { get; set; }

        /// <summary>
        /// Chave estrangeira para a entidade WorkCenter.
        /// </summary>
        [ForeignKey("WorkCenter")]
        public int WorkCenterID { get; set; }
        /// <summary>
        /// Propriedade de navegação para o Centro de Trabalho referenciado nesta etapa da rota.
        /// </summary>
        public virtual WorkCenter WorkCenter { get; set; }
    }
}
