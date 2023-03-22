using System;

namespace BH.DIS.Core.Messages
{
    public class NextDeferredException : Exception
    {
        public NextDeferredException(string message) : base(message)
        {
        }
    }
}