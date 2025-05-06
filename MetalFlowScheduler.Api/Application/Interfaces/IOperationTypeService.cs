// Arquivo: Domain/Interfaces/IOperationTypeService.cs
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
        /// Obtém um tipo de operação pelo seu ID. Lança NotFoundException se não encontrado ou inativo.
        /// </summary>
        /// <param name="id">O ID do tipo de operação.</param>
        /// <returns>O DTO do tipo de operação encontrado.</returns>
        /// <exception cref="NotFoundException">Lançada se o tipo de operação não for encontrado ou estiver inativo.</exception>
        Task<OperationTypeDto> GetByIdAsync(int id);

        /// <summary>
        /// Cria um novo tipo de operação. Lança ConflictException se o nome já existir.
        /// </summary>
        /// <param name="createDto">Os dados para criar o novo tipo de operação.</param>
        /// <returns>O DTO do tipo de operação criado.</returns>
        /// <exception cref="ConflictException">Lançada se já existir um tipo de operação ativo com o mesmo nome.</exception>
        Task<OperationTypeDto> CreateAsync(CreateOperationTypeDto createDto);

        /// <summary>
        /// Atualiza um tipo de operação existente. Lança NotFoundException se não encontrado, ConflictException se inativo ou nome duplicado.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser atualizado.</param>
        /// <param name="updateDto">Os novos dados para o tipo de operação.</param>
        /// <returns>O DTO do tipo de operação atualizado.</returns>
        /// <exception cref="NotFoundException">Lançada se o tipo de operação não for encontrado.</exception>
        /// <exception cref="ConflictException">Lançada se o tipo de operação estiver inativo ou se já existir outro tipo de operação ativo com o mesmo nome.</exception>
        Task<OperationTypeDto> UpdateAsync(int id, UpdateOperationTypeDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) um tipo de operação. Lança NotFoundException se não encontrado.
        /// </summary>
        /// <param name="id">O ID do tipo de operação a ser desabilitado.</param>
        /// <returns>True se a operação foi desabilitada ou já estava inativa.</returns>
        /// <exception cref="NotFoundException">Lançada se o tipo de operação não for encontrado.</exception>
        /// <exception cref="ConflictException">Lançada se houver dependências ativas (se implementado).</exception>
        Task<bool> DeleteAsync(int id);
    }
}
