﻿using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    internal class PortfolioAddedToSupervision : IEvent
    {
        public PortfolioAddedToSupervision()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public PortfolioAddedToSupervision(string portfolioName) : this()
        {
            PortfolioNumber = portfolioName;
        }

        public string PortfolioNumber { get; set; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }

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