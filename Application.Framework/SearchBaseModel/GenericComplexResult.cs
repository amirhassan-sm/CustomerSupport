using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Framework.SearchBaseModel
{
    public class GenericComplexResult<TSearchModel, TListIteam>
    {
        public TSearchModel? SearchModel { get; set; }

        public List<TListIteam> ListIteams { get; set; } = new List<TListIteam>();

    }
}
