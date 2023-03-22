using BH.DIS.Core.Events;
using System.Collections.Generic;

namespace BH.DIS.Core.Endpoints
{
    public interface IEndpoint
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        string Namespace { get; }
        string SecurityGroupName { get; }
        ISystem System { get; }

        /// <summary>
        /// Event types produced by the endpoint.
        /// </summary>
        IEnumerable<IEventType> EventTypesProduced { get; }

        /// <summary>
        /// Event types consumed by the endpoint.
        /// </summary>
        IEnumerable<IEventType> EventTypesConsumed { get; }

        IEnumerable<IRoleAssignment> RoleAssignments { get; }
    }
}
