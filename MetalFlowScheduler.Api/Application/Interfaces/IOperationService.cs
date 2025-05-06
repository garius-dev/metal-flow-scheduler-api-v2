// Arquivo: Application/Interfaces/IOperationService.cs
using MetalFlowScheduler.Api.Application.Dtos;
using MetalFlowScheduler.Api.Application.Exceptions; // Importar as exceções customizadas
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
        /// Obtém uma operação pelo seu ID. Lança NotFoundException se não encontrado ou inativo.
        /// </summary>
        /// <param name="id">O ID da operação.</param>
        /// <returns>O DTO da operação encontrada.</returns>
        /// <exception cref="NotFoundException">Lançada se a operação não for encontrada ou estiver inativa.</exception>
        Task<OperationDto> GetByIdAsync(int id);

        /// <summary>
        /// Cria uma nova operação. Lança ConflictException se o nome já existir, ValidationException se entidades relacionadas forem inválidas.
        /// </summary>
        /// <param name="createDto">Os dados para criar a nova operação.</param>
        /// <returns>O DTO da operação criada.</returns>
        /// <exception cref="ConflictException">Lançada se já existir uma operação ativa com o mesmo nome.</exception>
        /// <exception cref="ValidationException">Lançada se o Tipo de Operação ou Centro de Trabalho associados forem inválidos.</exception>
        Task<OperationDto> CreateAsync(CreateOperationDto createDto);

        /// <summary>
        /// Atualiza uma operação existente. Lança NotFoundException se não encontrado, ConflictException se inativo ou nome duplicado, ValidationException se entidades relacionadas forem inválidas.
        /// </summary>
        /// <param name="id">O ID da operação a ser atualizada.</param>
        /// <param name="updateDto">Os novos dados para a operação.</param>
        /// <returns>O DTO da operação atualizada.</returns>
        /// <exception cref="NotFoundException">Lançada se a operação não for encontrada.</exception>
        /// <exception cref="ConflictException">Lançada se a operação estiver inativa ou se já existir outra operação ativa com o mesmo nome.</exception>
        /// <exception cref="ValidationException">Lançada se o Tipo de Operação ou Centro de Trabalho associados forem inválidos.</exception>
        Task<OperationDto> UpdateAsync(int id, UpdateOperationDto updateDto);

        /// <summary>
        /// Desabilita (soft delete) uma operação. Lança NotFoundException se não encontrado, ConflictException se houver dependências ativas.
        /// </summary>
        /// <param name="id">O ID da operação a ser desabilitada.</param>
        /// <returns>True se a operação foi desabilitada ou já estava inativa.</returns>
        /// <exception cref="NotFoundException">Lançada se a operação não for encontrada.</exception>
        /// <exception cref="ConflictException">Lançada se houver dependências ativas (se implementado).</exception>
        Task<bool> DeleteAsync(int id);
    }
}