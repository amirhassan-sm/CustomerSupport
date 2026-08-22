using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customer.DomainServiceContract.BaseInterface
{
    public interface ICrudBaseInterface<TModel,TId>
    {
        Task<TModel?> GetByIdAsync(TId id);
        Task AddAsync(TModel model);

        void Update(TModel model);

        void Delete(TModel model);
    }
}
