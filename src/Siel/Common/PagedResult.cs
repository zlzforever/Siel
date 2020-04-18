using System.Collections.Generic;

namespace Siel.Common
{
    public class PagedResult<T>
    {
        public int Count { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public List<T> Data { get; set; }
    }
}