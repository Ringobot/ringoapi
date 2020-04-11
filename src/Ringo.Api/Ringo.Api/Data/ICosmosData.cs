using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ringo.Api.Data
{
    public interface ICosmosData<T> where T : ICosmosModel
    {
        Task<T> Create(T item);
        Task Delete(string id, string pk, string eTag);
        Task<T> Get(string id, string pk);
        Task<T> GetOrDefault(string id, string pk);
        Task<IEnumerable<T>> GetAll();
        Task<T> Replace(T item, string ifMatch);
    }
}