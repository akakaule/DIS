﻿using System;
using System.Runtime.Serialization;

namespace BH.DIS.Core.Messages.Exceptions
{
    [Serializable]
    public class InvalidMessageException : Exception
    {
        public InvalidMessageException()
        {
        }

        public InvalidMessageException(string message) : base(message)
        {
        }

        public InvalidMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}