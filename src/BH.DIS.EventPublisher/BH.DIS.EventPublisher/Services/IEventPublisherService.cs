using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BH.DIS.EventPublisher.Models;

namespace BH.DIS.EventPublisher.Services
{
    public interface IEventPublisherService
    {
        Task Handle(EventPublisherModel eventModel);
    }
}
