using System;

namespace BH.DIS.Core.Messages.Exceptions
{
    public class UnsupportedMessageTypeException : Exception
    {
        public UnsupportedMessageTypeException(MessageType messageType) : base($"Unsupported MessageType: '{messageType}'.")
        {
        }
    }
}