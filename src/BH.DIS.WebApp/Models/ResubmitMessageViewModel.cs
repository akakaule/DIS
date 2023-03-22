using System.Collections.Generic;
using BH.DIS.Core.Events;
using BH.DIS.WebApp.Constants;

namespace BH.DIS.WebApp.Models
{
    public class ResubmitMessageViewModel : ComposeNewMessageViewModel
    {
        public string ErrorMessageId { get; set; }
    }
}
