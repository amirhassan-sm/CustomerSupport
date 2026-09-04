using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Framework.SearchBaseModel
{
    public class PageModel
    {
        private int _PageIndex = 1;

        public int pageIndex
        {
            get => _PageIndex;
            set => _PageIndex = value < 1 ? 1 : value;
        }

        private int _pageSize { get; set; }

        public int pageSize
        {
            get => _pageSize == 0 ? 10 : _pageSize;

            set => _pageSize = (value == 0 ? _pageSize = 10 : _pageSize = value);
        }

        private int _recordCount { get; set; }
        public int RecordCount { get => _recordCount; set => _recordCount = value; }



        public int pageCount
        {
            get
            {
                if (RecordCount % pageSize == 0)
                {
                    return RecordCount / pageSize;

                }
                else
                {
                    return (RecordCount / pageSize) + 1;
                }
            }
        }
    }
}
