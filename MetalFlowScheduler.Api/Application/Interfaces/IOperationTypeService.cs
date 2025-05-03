using MetalFlowScheduler.Api.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de Tipos de Operação.
    /// </summary>
    public interface IOperationTypeService
    {
        /// <summary>
        /// Obtém todos os tipos de operação ativos.
        /// </summary>
        Task<IEnumerable<OperationTypeDto>> GetAllEnabledAsync();

        /// <summary>
        /// Obtém um tipo de operação pelo seu ID. Retorna null se não encontrado ou inativo.
        /// </summary>
        Task<OperationTypeDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria um novo tipo de operação.
        /// </summary>
        Task<OperationTypeDto> CreateAsync(CreateOperationTypeDto createDto);

        /// <summary>
        /// Atualiza um tipo de operação existente. Retorna null se não encontrado.
        /// </summary>
        Task<OperationTypeDto?> UpdateAsync(int id, UpdateOperationTypeDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) um tipo de operação. Retorna true se bem-sucedido, false caso contrário.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
