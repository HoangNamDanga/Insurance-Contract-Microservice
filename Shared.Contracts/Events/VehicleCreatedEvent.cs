using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Events
{
    public class VehicleCreatedEvent
    {
        public int VehicleId { get; set; }
        public int PolicyId { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int YearManufactured { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
