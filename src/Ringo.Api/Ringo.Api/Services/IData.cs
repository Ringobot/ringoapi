using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    internal interface IData<T>
    {
        Task<T> Get(string id);
    }
}