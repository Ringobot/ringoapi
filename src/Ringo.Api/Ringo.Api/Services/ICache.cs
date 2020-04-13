using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ringo.Api.Services
{
    public interface ICache
    {
        Task<T> Get<T>(string key);

        Task Set(string key, object value, TimeSpan timeSpan);
    }
}
