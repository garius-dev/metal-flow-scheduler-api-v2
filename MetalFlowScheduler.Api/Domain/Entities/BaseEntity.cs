using System.ComponentModel.DataAnnotations;

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Classe base abstrata para todas as entidades do domínio.
    /// Fornece propriedades comuns como ID, status de ativação e timestamps.
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// Identificador único da entidade. Chave primária.
        /// </summary>
        [Key]
        public int ID { get; set; }

        /// <summary>
        /// Indica se a entidade está ativa (true) ou inativa (false - soft delete).
        /// O padrão é true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Data e hora (UTC) em que a entidade foi criada.
        /// O padrão é a hora UTC atual.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data e hora (UTC) da última atualização da entidade.
        /// O padrão é a hora UTC atual.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }
}
