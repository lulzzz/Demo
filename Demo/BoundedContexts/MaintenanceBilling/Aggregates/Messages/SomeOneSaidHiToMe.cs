using System;
using Demo.BoundedContexts.MaintenanceBilling.Events;

namespace Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class SomeOneSaidHiToMe : IEvent
    {
        public SomeOneSaidHiToMe(string accountNumber, string debuInfo)
        {
            AccountNumber = accountNumber;
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            DebugInfo = debuInfo;
        }

        private DateTime _OccurredOn { get; }
        public string AccountNumber { get; }
        private Guid _UniqueGuid { get; }

        public string DebugInfo { get; }

        public DateTime OccurredOn()
        {
            return _OccurredOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}