using System;
using System.Collections.Immutable;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountState
    {

        /**
         * Only two ways to initiate an Account State
         * All modifications to it must be immutable and done by the ApplyEvent() handler
         */
        public AccountState()
        {
            Obligations = ImmutableDictionary.Create<string, MaintenanceFee>();
            SimulatedFields = ImmutableDictionary.Create<string, string>();
            AuditLog = ImmutableList.Create<StateLog>();
        }

        public AccountState(string accountNumber) : this()
        {
            AccountNumber = accountNumber;
        }


        /**
         * 
         * Private constructors, only to be used by the ApplyEvent method 
         * 
        */
        /**private AccountState(
            string accountNumber,
            double currentBalance,
            ImmutableDictionary<string, string> simulation,
            ImmutableList<StateLog> log,
            double openingBalance,
            string inventory,
            string userName,
            double lastPaymentAmount,
            DateTime lastPaymentDate)
        {
            CurrentBalance = currentBalance;
            SimulatedFields = simulation;
            AccountNumber = accountNumber;
            AuditLog = log;
            Obligations = ImmutableDictionary.Create<string, MaintenanceFee>();
            OpeningBalance = openingBalance;
            Inventroy = inventory;
            UserName = userName;
            LastPaymentAmount = lastPaymentAmount;
            LastPaymentDate = lastPaymentDate;
        }**/

        /**private AccountState(
            string accountNumber,
            double currentBalance,
            AccountStatus accountStatus,
            ImmutableDictionary<string, MaintenanceFee> obligations,
            ImmutableDictionary<string, string> simulation,
            double openingBalance,
            string inventory,
            string userName,
            double lastPaymentAmount,
            DateTime lastPaymentDate)
        {
            AccountNumber = accountNumber;
            CurrentBalance = currentBalance;
            AccountStatus = accountStatus;
            Obligations = obligations;
            SimulatedFields = simulation;
            Inventroy = inventory;
            UserName = userName;
            LastPaymentAmount = lastPaymentAmount;
            LastPaymentDate = lastPaymentDate;
            OpeningBalance = openingBalance;
        }**/

        private AccountState(string accountNumber,
            double currentBalance,
            AccountStatus accountStatus,
            ImmutableDictionary<string, MaintenanceFee> obligations,
            ImmutableDictionary<string, string> simulation,
            ImmutableList<StateLog> log,
            double openingBalance,
            string inventory,
            string userName,
            double lastPaymentAmount,
            DateTime lastPaymentDate)
        {
            AccountNumber = accountNumber;
            CurrentBalance = currentBalance;
            AccountStatus = accountStatus;
            Obligations = obligations;
            AuditLog = log;
            SimulatedFields = simulation;
            Inventroy = inventory;
            UserName = userName;
            LastPaymentAmount = lastPaymentAmount;
            LastPaymentDate = lastPaymentDate;
            OpeningBalance = openingBalance;
        }


        public string UserName { get;   }

        public string Inventroy { get;  }

        public double OpeningBalance { get; }

        public string AccountNumber { get; }

        public double CurrentBalance { get; }

        public AccountStatus AccountStatus { get; private set; }

        public ImmutableList<StateLog> AuditLog { get; }

        public ImmutableDictionary<string, string> SimulatedFields { get; }

        public ImmutableDictionary<string, MaintenanceFee> Obligations { get; }

        /**
         * The ApplyEvent() handler is responsible for always returning a new state
         */
        public AccountState ApplyEvent(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case AccountCurrentBalanceUpdated occurred:
                    return ApplyEvent(occurred);
                case AccountStatusChanged occurred:
                    return ApplyEvent(occurred);
                case AccountCancelled occurred:
                    return ApplyEvent(occurred);
                case ObligationAddedToAccount occurred:
                    return ApplyEvent(occurred);
                case ObligationAssessedConcept occurred:
                    return ApplyEvent(occurred);
                case ObligationSettledConcept occurred:
                    return ApplyEvent(occurred);
                case AccountCreated occurred:
                    return ApplyEvent(occurred);
                case SuperSimpleSuperCoolDomainEventFoundByRules occurred:
                    return ApplyEvent(occurred);
                case TaxAppliedDuringBilling occurred:
                    return ApplyEvent(occurred);
                case UacAppliedAfterBilling occurred:
                    return ApplyEvent(occurred);
                case AccountBusinessRuleValidationSuccess occurred:
                    return ApplyEvent(occurred);


                default:
                    throw new UnknownAccountEventException($"{domainEvent.GetType()}");
            }
        }

        private AccountState ApplyEvent(AccountBusinessRuleValidationSuccess occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountBusinessRuleValidationSuccess", $"AccountBusinessRuleValidationSuccess on {occurred.Message}",
                        occurred.UniqueGuid(),
                        occurred.OccurredOn()
                    )
                ),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate
            );
        }

        private AccountState ApplyEvent(UacAppliedAfterBilling occurred)
        {
            var trans = new FinancialTransaction(new Tax() {Amount = occurred.UacAmountApplied},
                occurred.UacAmountApplied);
            Obligations[occurred.ObligationNumber]?.PostTransaction(trans);
            var newState = new AccountState(
                accountNumber: AccountNumber, 
                currentBalance: CurrentBalance + (-1 * occurred.UacAmountApplied),
                accountStatus: AccountStatus, 
                obligations: Obligations,
                simulation: SimulatedFields,
                log: AuditLog.Add(new StateLog("UacAppliedAfterBilling", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate
            );
            //Console.WriteLine($"ObligationAssessedConcept: {occurred}");
            //Console.WriteLine($"New AccountState: {newState}");

            return newState;
        }

        private AccountState ApplyEvent(TaxAppliedDuringBilling occurred)
        {
            var trans = new FinancialTransaction(new Tax() {Amount = occurred.TaxAmountApplied},
                occurred.TaxAmountApplied);
            Obligations[occurred.ObligationNumber]?.PostTransaction(trans);
            var newState = new AccountState(
                accountNumber: AccountNumber,
                currentBalance: CurrentBalance + occurred.TaxAmountApplied,
                accountStatus: AccountStatus,
                obligations: Obligations,
                simulation: SimulatedFields,
                log: AuditLog.Add(new StateLog("TaxAppliedDuringBilling", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
            //Console.WriteLine($"ObligationAssessedConcept: {occurred}");
            //Console.WriteLine($"New AccountState: {newState}");

            return newState;
        }

        public static AccountState Clone(AccountState state)
        {
            return new AccountState(
                accountNumber: state.AccountNumber
                , currentBalance: state.CurrentBalance
                , accountStatus: state.AccountStatus
                , obligations: state.Obligations
                , simulation: state.SimulatedFields
                , log: state.AuditLog
                , openingBalance: state.OpeningBalance,
                inventory: state.Inventroy,
                userName: state.UserName,
                lastPaymentAmount: state.LastPaymentAmount,
                lastPaymentDate: state.LastPaymentDate);

        }

        private AccountState ApplyEvent(SomeOneSaidHiToMe occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus,
                Obligations,
                LoadSimulation().ToImmutableDictionary(),
                AuditLog.Add(new StateLog("SomeOneSaidHiToMe", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);

        }

        private AccountState ApplyEvent(SuperSimpleSuperCoolDomainEventFoundByRules occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus, Obligations,
                LoadSumulation(SimulatedFields, "1", "My state has been updated, see..."),
                AuditLog.Add(new StateLog("SuperSimpleSuperCoolEventFoundByRules", occurred.Message,
                    occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(ObligationAssessedConcept occurred)
        {
            var trans = new FinancialTransaction(occurred.FinancialBucket, occurred.FinancialBucket.Amount);
            Obligations[occurred.ObligationNumber]?.PostTransaction(trans);
            var newState = new AccountState(
                AccountNumber,
                CurrentBalance + occurred.FinancialBucket.Amount,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("ObligationAssessedConcept", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
            
            
            return newState;
        }

        private AccountState ApplyEvent(AccountCurrentBalanceUpdated occurred)
        {
            return new AccountState(
                AccountNumber,
                occurred.CurrentBalance,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountCurrentBalanceUpdated", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(AccountStatusChanged occurred)
        {
            return new AccountState(
                accountNumber: AccountNumber,
                currentBalance: CurrentBalance,
                accountStatus: occurred.AccountStatus, 
                obligations: Obligations,
                simulation: SimulatedFields,
                log: AuditLog.Add(new StateLog("AccountStatusChanged", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(AccountCancelled occurred)
        {
            return new AccountState(AccountNumber, 
                CurrentBalance,
                occurred.AccountStatus, Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountCancelled", occurred.Message, occurred.UniqueGuid(), occurred.OccurredOn()))
                ,openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(ObligationAddedToAccount occurred)
        {
            return new AccountState(
                AccountNumber, 
                CurrentBalance,
                AccountStatus,
                Obligations.Add(occurred.MaintenanceFee.ObligationNumber, occurred.MaintenanceFee),
                SimulatedFields,
                AuditLog.Add(new StateLog("ObligationAddedToAccount", occurred.Message, occurred.UniqueGuid(), occurred.OccurredOn()))
                ,openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(ObligationSettledConcept occurred)
        {
            var trans = new FinancialTransaction(occurred.FinancialBucket, occurred.Amount);
            Obligations[occurred.ObligationNumber].PostTransaction(trans);
            return new AccountState(
                AccountNumber, 
                CurrentBalance,
                AccountStatus, 
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("ObligationSettledConcept", occurred.Message, occurred.UniqueGuid(), occurred.OccurredOn()))
                ,openingBalance: OpeningBalance,
                inventory: Inventroy,
                userName: UserName,
                lastPaymentAmount: LastPaymentAmount,
                lastPaymentDate: LastPaymentDate);
        }

        private AccountState ApplyEvent(AccountCreated occurred)
        {

            var newState = new AccountState(
                accountNumber: occurred.AccountNumber,
                accountStatus: AccountStatus.Boarded,
                obligations: ImmutableDictionary.Create<string, MaintenanceFee>(),
                simulation: LoadSimulation(),
                log: AuditLog.Add(
                    new StateLog("AccountCreated",
                        occurred.Message,
                        occurred.UniqueGuid(),
                        occurred.OccurredOn()
                    )
                ),
                openingBalance: occurred.OpeningBalance,
                currentBalance: occurred.OpeningBalance,
                inventory: occurred.Inventory,
                userName: occurred.UserName, 
                lastPaymentAmount: occurred.LastPaymentAmount,
                lastPaymentDate: occurred.LastPaymentDate
            );
           
            return newState;
        }
        
        public double LastPaymentAmount { get; }
        public DateTime LastPaymentDate { get; }

        /* Helpers */
        private static ImmutableDictionary<string, string> LoadSimulation()
        {
            var range = ImmutableDictionary.Create<string, string>();
            for (var i = 1; i <= 100; i++)
                range = range.Add(i.ToString(), $"This simulates field {i}");

            return range;
        }

        private static ImmutableDictionary<string, string> LoadSumulation(ImmutableDictionary<string, string> state,
            string keyToUpdate,
            string valueToUpdate)
        {
            state = state.SetItem(keyToUpdate, valueToUpdate);
            return state;
        }

        public override string ToString()
        {
            return
                $"[AccountState: Obligations={Obligations}, AccountNumber={AccountNumber}, CurrentBalance={CurrentBalance}, DebugInfo={AuditLog}, SimulatedFields={SimulatedFields}]";
        }
    }
}