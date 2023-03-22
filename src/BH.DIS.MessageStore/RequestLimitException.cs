using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.MessageStore
{
    public class RequestLimitException : Exception
    {
        public RequestLimitException()
        {
        }

        public RequestLimitException(string message)
            : base(message)
        {
        }

        public RequestLimitException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
