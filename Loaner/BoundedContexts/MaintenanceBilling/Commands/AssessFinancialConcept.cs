﻿using System;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Commands
{
    public class AssessFinancialConcept : IDomainCommand
    {
        public AssessFinancialConcept()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public AssessFinancialConcept(string obligationNumber, IFinancialBucket bucket, double amount) : this()
        {
            ObligationNumber = obligationNumber;
            FinancialBucket = bucket;
            Amount = amount;
        }

        public string ObligationNumber { get; }
        public double Amount { get; }
        public IFinancialBucket FinancialBucket { get; }

        private DateTime _RequestedOn { get; }
        private Guid _UniqueGuid { get; }

        public DateTime RequestedOn()
        {
            return _RequestedOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}