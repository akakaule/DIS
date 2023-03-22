using System;

namespace BH.DIS.Core.Messages
{
    public class SessionBlockedException : Exception
    {
        public SessionBlockedException(string message) : base(message)
        {

        }

        public SessionBlockedException() : base("Session is blocked.")
        {

        }
    }
}