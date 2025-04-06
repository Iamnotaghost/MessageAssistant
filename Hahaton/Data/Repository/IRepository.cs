// Data/Repository/IRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileAssistant.Data.Repository
{
    public interface IRepository<T>
    {
        /// <summary>
        /// Получить элемент по идентификатору
        /// </summary>
        Task<T> GetByIdAsync(string id);

        /// <summary>
        /// Получить все элементы
        /// </summary>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Добавить новый элемент
        /// </summary>
        Task AddAsync(T entity);

        /// <summary>
        /// Обновить существующий элемент
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Удалить элемент по идентификатору
        /// </summary>
        Task DeleteAsync(string id);
    }
}