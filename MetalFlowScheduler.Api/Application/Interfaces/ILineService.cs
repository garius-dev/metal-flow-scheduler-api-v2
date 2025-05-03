using MetalFlowScheduler.Api.Application.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Application.Interfaces
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de Linhas de Produção.
    /// </summary>
    public interface ILineService
    {
        /// <summary>
        /// Obtém todas as linhas de produção ativas.
        /// </summary>
        Task<IEnumerable<LineDto>> GetAllEnabledAsync();

        /// <summary>
        /// Obtém uma linha de produção pelo seu ID. Retorna null se não encontrado ou inativo.
        /// </summary>
        Task<LineDto?> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma nova linha de produção, incluindo suas rotas de centros de trabalho e produtos disponíveis.
        /// </summary>
        Task<LineDto> CreateAsync(CreateLineDto createDto);

        /// <summary>
        /// Atualiza uma linha de produção existente, gerenciando suas rotas e produtos disponíveis. Retorna null se não encontrado.
        /// </summary>
        Task<LineDto?> UpdateAsync(int id, UpdateLineDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) uma linha de produção. Retorna true se bem-sucedido, false caso contrário.
        /// </summary>
        Task<bool> DeleteAsync(int id);
    }
}
