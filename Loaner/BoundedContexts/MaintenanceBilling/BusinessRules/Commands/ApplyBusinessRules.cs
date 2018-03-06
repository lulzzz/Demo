using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using NLog.Web.LayoutRenderers;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class ApplyBusinessRules : ICloneable
    {
        public ApplyBusinessRules(double totalBilledAmount, string client, string portfolioName,
            AccountState accountState, BillingAssessment command, IActorRef accountBusinessMapperRouter, IActorRef accountRef)
        {
            TotalBilledAmount = totalBilledAmount;
            Client = client;
            PortfolioName = portfolioName;
            AccountState = accountState;
            Command = command;
            AccountBusinessMapperRouter = accountBusinessMapperRouter;
            AccountRef = accountRef;
        }

        public double TotalBilledAmount { get; private set; } //TODO tis has to be moved somewhere else
        public string Client { get; private set; }
        public string PortfolioName { get; private set; }
        public AccountState AccountState { get; private set; }
        public BillingAssessment Command { get; set; }
        public IActorRef AccountBusinessMapperRouter { get; private set; }
        public IActorRef AccountRef { get; private set; }

        private ApplyBusinessRules(ApplyBusinessRules abr)
        {
            TotalBilledAmount = abr.TotalBilledAmount;
            Client = abr.Client;
            PortfolioName = abr.PortfolioName;
            AccountState = abr.AccountState;
            Command = abr.Command;
            AccountBusinessMapperRouter = abr.AccountBusinessMapperRouter;
            AccountRef = abr.AccountRef;
        }

        public override string ToString()
        {
            return 
                $"TotalBilledAmount {TotalBilledAmount } \n" +
                $"Client {Client } \n" +
                $"PortfolioName {PortfolioName } \n" +
                $"AccountState {AccountState} \n" +
                $"Command {Command} \n" +
                $"AccountBusinessMapperRouter {AccountBusinessMapperRouter}\n" +
                $"AccountRef {AccountRef.Path.Name}";
        }

        public object Clone()
        {
            return new ApplyBusinessRules(this);
        }
    }
}