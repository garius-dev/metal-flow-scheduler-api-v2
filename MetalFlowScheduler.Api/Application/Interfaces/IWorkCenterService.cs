using MetalFlowScheduler.Api.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de Centros de Trabalho.
    /// </summary>
    public interface IWorkCenterService
    {
        /// <summary>
        /// Obtém todos os centros de trabalho ativos.
        /// </summary>
        Task<IEnumerable<WorkCenterDto>> GetAllEnabledAsync();

        /// <summary>
        /// Obtém um centro de trabalho pelo seu ID. Retorna null se não encontrado ou inativo.
        /// </summary>
        Task<WorkCenterDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria um novo centro de trabalho, incluindo suas rotas de operação.
        /// </summary>
        Task<WorkCenterDto> CreateAsync(CreateWorkCenterDto createDto);

        /// <summary>
        /// Atualiza um centro de trabalho existente, gerenciando suas rotas de operação. Retorna null se não encontrado.
        /// </summary>
        Task<WorkCenterDto?> UpdateAsync(int id, UpdateWorkCenterDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) um centro de trabalho. Retorna true se bem-sucedido, false caso contrário.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
