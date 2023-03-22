using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using BH.DIS.Core.Events;

namespace BH.DIS.Validation
{
    class EventValidation
    {
        internal static void Validate(Event eventObject)
        {
            try
            {
                Validator.ValidateObject(eventObject, new ValidationContext(eventObject), true);
            }
            catch (System.ComponentModel.DataAnnotations.ValidationException e)
            {
                throw new InvalidEventException(eventObject.GetType().Name, e);
            }
        }
    }
}
