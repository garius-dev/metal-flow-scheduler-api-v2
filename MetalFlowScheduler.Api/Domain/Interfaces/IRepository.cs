using MetalFlowScheduler.Api.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MetalFlowScheduler.Api.Domain.Interfaces
{
    /// <summary>
    /// Interface genérica para o padrão Repository.
    /// Define as operações básicas de CRUD e outras operações comuns para todas as entidades.
    /// </summary>
    /// <typeparam name="T">O tipo da entidade, que deve herdar de BaseEntity.</typeparam>
    public interface IRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// Obtém uma entidade pelo seu ID de forma assíncrona.
        /// </summary>
        /// <param name="id">O ID da entidade a ser encontrada.</param>
        /// <returns>
        /// Uma tarefa que representa a operação assíncrona.
        /// O resultado da tarefa contém a entidade encontrada, ou null se não for encontrada.
        /// </returns>
        Task<T?> GetByIdAsync(int id);

        /// <summary>
        /// Obtém todas as entidades do tipo T de forma assíncrona.
        /// </summary>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém uma lista de todas as entidades.</returns>
        Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Obtém todas as entidades ativas (Enabled = true) do tipo T de forma assíncrona.
        /// </summary>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém uma lista das entidades ativas.</returns>
        Task<IEnumerable<T>> GetAllEnabledAsync();

        /// <summary>
        /// Encontra entidades do tipo T que satisfazem um predicado especificado de forma assíncrona.
        /// </summary>
        /// <param name="predicate">A expressão lambda para filtrar as entidades.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém uma lista das entidades encontradas.</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Adiciona uma nova entidade ao repositório de forma assíncrona.
        /// </summary>
        /// <param name="entity">A entidade a ser adicionada.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task AddAsync(T entity);

        /// <summary>
        /// Adiciona uma coleção de novas entidades ao repositório de forma assíncrona.
        /// </summary>
        /// <param name="entities">A coleção de entidades a ser adicionada.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task AddRangeAsync(IEnumerable<T> entities);

        /// <summary>
        /// Atualiza uma entidade existente no repositório de forma assíncrona.
        /// </summary>
        /// <param name="entity">A entidade a ser atualizada.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Remove permanentemente uma entidade do repositório de forma assíncrona.
        /// </summary>
        /// <param name="entity">A entidade a ser removida.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task RemoveAsync(T entity);

        /// <summary>
        /// Marca uma entidade como inativa (Enabled = false) no repositório de forma assíncrona (soft delete).
        /// </summary>
        /// <param name="entity">A entidade a ser marcada como inativa.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task SoftRemoveAsync(T entity);

        /// <summary>
        /// Remove permanentemente uma coleção de entidades do repositório de forma assíncrona.
        /// </summary>
        /// <param name="entities">A coleção de entidades a ser removida.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        Task RemoveRangeAsync(IEnumerable<T> entities);
    }
}
