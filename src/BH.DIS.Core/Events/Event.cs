using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BH.DIS.Core.Events
{
    public abstract class Event : IEvent
    {
        public IEvent.EventValidationResult TryValidate()
        {
            var validationResults = new List<ValidationResult>();
            return new IEvent.EventValidationResult(Validator.TryValidateObject(this, new ValidationContext(this), validationResults, true), validationResults);
        }

        public void Validate()
        {
            Validator.ValidateObject(this, new ValidationContext(this),true);
        }

        public IEventType GetEventType() => new EventType(GetType());

        public virtual string GetSessionId() => Guid.NewGuid().ToString();

 
    }
}
