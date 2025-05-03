using MetalFlowScheduler.Api.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de Operações.
    /// </summary>
    public interface IOperationService
    {
        /// <summary>
        /// Obtém todas as operações ativas.
        /// </summary>
        Task<IEnumerable<OperationDto>> GetAllEnabledAsync();

        /// <summary>
        /// Obtém uma operação pelo seu ID. Retorna null se não encontrado ou inativo.
        /// </summary>
        Task<OperationDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma nova operação.
        /// </summary>
        Task<OperationDto> CreateAsync(CreateOperationDto createDto);

        /// <summary>
        /// Atualiza uma operação existente. Retorna null se não encontrado.
        /// </summary>
        Task<OperationDto?> UpdateAsync(int id, UpdateOperationDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) uma operação. Retorna true se bem-sucedido, false caso contrário.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
