using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Necessário para ICollection
using System; // Necessário para DateTime

namespace MetalFlowScheduler.Api.Domain.Entities
{
    /// <summary>
    /// Representa uma ordem de produção, contendo um ou mais itens a serem produzidos.
    /// </summary>
    [Table("ProductionOrders")]
    public class ProductionOrder : BaseEntity
    {
        /// <summary>
        /// Número identificador da ordem de produção.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string OrderNumber { get; set; }

        /// <summary>
        /// Data/hora mais cedo em que esta ordem pode começar a ser produzida.
        /// </summary>
        [Required]
        public DateTime EarliestStartDate { get; set; }

        /// <summary>
        /// Data/hora limite (prazo) para a conclusão de TODOS os itens desta ordem.
        /// </summary>
        [Required]
        public DateTime Deadline { get; set; }

        /// <summary>
        /// Coleção de itens que compõem esta ordem de produção.
        /// </summary>
        public virtual ICollection<ProductionOrderItem> Items { get; set; } = new List<ProductionOrderItem>();

        /// <summary>
        /// Construtor padrão necessário para EF Core e desserialização.
        /// </summary>
        public ProductionOrder()
        {
            Enabled = true;
        }

        /// <summary>
        /// Construtor para criar uma nova ordem de produção com dados iniciais.
        /// </summary>
        /// <param name="orderNumber">Número da ordem.</param>
        /// <param name="earliestStartDate">Data mais cedo para início.</param>
        /// <param name="deadline">Prazo final.</param>
        public ProductionOrder(string orderNumber, DateTime earliestStartDate, DateTime deadline)
        {
            OrderNumber = orderNumber;
            EarliestStartDate = earliestStartDate;
            Deadline = deadline;
            Enabled = true;
        }
    }
}
