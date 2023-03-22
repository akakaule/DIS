using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BH.DIS.Validation
{
    public class InvalidEventException : Exception
    {
        public InvalidEventException(string eventTypeName, ValidationException validationException)
            : base($"Invalid {eventTypeName} event. {validationException.ValidationResult.ErrorMessage.TrimEnd('.')}. Value was: '{(validationException.Value == null ? "null" : validationException.Value.ToString())}'.", validationException)
        {

        }
    }
}
