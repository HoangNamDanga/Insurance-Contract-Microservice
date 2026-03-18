using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class VehicleDeletedEvent
    {
        public int VehicleId { get; init; }
        public int PolicyId { get; init; }
        public DateTime DeletedAt { get; init; } = DateTime.UtcNow;
    }
}
