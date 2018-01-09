using System;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Demo.BoundedContexts.MaintenanceBilling.BusinessRules;
using Demo.BoundedContexts.MaintenanceBilling.Commands;
using Demo.BoundedContexts.MaintenanceBilling.Events;
using static Demo.ActorManagement.LoanerActors;

//using Demo.Custom.Persistence;

namespace Demo.BoundedContexts.MaintenanceBilling.Aggregates
{
    public class AccountActor : ReceivePersistentActor 
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /* This Actor's State */
        private AccountState _accountState = new AccountState();

        private DateTime _lastBootedOn;

        //private OneDatabasePerActor db;
        public AccountActor()
        {
            
            /* Hanlde Recovery */
            Recover<SnapshotOffer>(offer => offer.Snapshot is AccountState, offer => ApplySnapShot(offer));
            Recover<AccountCreated>(@event => ApplyPastEvent("AccountCreated", @event));
            Recover<ObligationAddedToAccount>(@event => ApplyPastEvent("ObligationAddedToAccount", @event));
            Recover<ObligationAssessedConcept>(@event => ApplyPastEvent("ObligationAssessedConcept", @event));
            Recover<SuperSimpleSuperCoolEventFoundByRules>(
                @event => ApplyPastEvent("SuperSimpleSuperCoolEventFoundByRules", @event));

            /**
             * Creating the account's initial state is more of a one-time thing 
             * For the demo there no business rules are assumed when adding an 
             * maintenanceFee to an account, but there most likely will be in reality
             * */
            Command<CreateAccount>(command => InitiateAccount(command));
            Command<AddObligationToAccount>(command => AddObligation(command));
            Command<CheckYoSelf>(command => RegisterStartup() /*effectively a noop */);
            
            /* Example of running comannds through business rules */
            Command<SettleFinancialConcept>(command => ApplyBusinessRules(command));
            Command<AssessFinancialConcept>(command => ApplyBusinessRules(command));
            Command<BillingAssessment>(command => ProcessBilling(command));
            Command<CancelAccount>(command => ApplyBusinessRules(command));
            Command<AskToBeSupervised>(command => SendParentMyState(command));

            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            //Command<RecoverySuccess>(msg => this.WakeUp());
            Command<TellMeYourStatus>(asking => Sender.Tell(new MyAccountStatus($"{Self.Path.Name} I am alive! I was last booted up on {_lastBootedOn}")));
            Command<TellMeYourInfo>(asking => Sender.Tell(new MyAccountStatus("", AccountState.Clone(_accountState) )));
            Command<DeleteMessagesSuccess>(
                msg => _log.Info($"Successfully cleared log after snapshot ({msg.ToString()})"));
            CommandAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            //DeleteMessages(snapshotSeqNr);
            //DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
        }


        public override string PersistenceId => Self.Path.Name;
      
        private void ProcessBilling(BillingAssessment command)
        {
            Sender.Tell(new MyAccountStatus($"Your billing request has been submitted.",AccountState.Clone(_accountState)));
            ApplyBusinessRules(command);
            Context.Parent.Tell(
                    new RegisterMyAccountBilling(_accountState.AccountNumber,
                            command.LineItems.Select(x => x.TotalAmount).Sum() ,
                            _accountState.CurrentBalance)
                );
        }

        private void SendParentMyState(AskToBeSupervised command)
        {
            Monitor();
            /* Assuming this is all we have to load for an account, then we can have the account
             * send the supervisor to add it to it's list -- then it can terminate. 
             */
            command.MyNewParent.Tell(new SuperviseThisAccount(command.Portfolio, Self.Path.Name));
            Self.Tell(PoisonPill.Instance);
        }

        private void ApplySnapShot(SnapshotOffer offer)
        {
            _accountState = (AccountState) offer.Snapshot;
            _log.Debug($"Snapshot recovered.");
        }

        private void ApplyPastEvent(string eventname, IEvent @event)
        {
            RecoveryCounter();
            _accountState = _accountState.ApplyEvent(@event);
            _log.Debug($"Recovery event: {eventname}");
        }

        private void AddObligation(AddObligationToAccount command)
        {
            Monitor();
            if (!_accountState.Obligations.ContainsKey(command.MaintenanceFee.ObligationNumber))
            {
                var @event = new ObligationAddedToAccount(command.AccountNumber, command.MaintenanceFee);
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);
                    ApplySnapShotStrategy();
                    _log.Debug(
                        $"Added maintenanceFee {command.MaintenanceFee.ObligationNumber} to account {command.AccountNumber}");
                    /* Optionally, put this command on the external notificaiton system (i.e. Kafka) */
                });
            }
            else
            {
                _log.Debug(
                    $"You are trying to add maintenanceFee {command.MaintenanceFee.ObligationNumber} an account which has exists on account {command.AccountNumber}. No action taken.");
            }
        }

        private void InitiateAccount(CreateAccount command)
        {
            Monitor();
            if (_accountState.AccountNumber == null)
            {
                /**
                 * we want to use behaviours here to make sure we don't allow the account to be created 
                 * once it has been created -- Become AccountBoarded perhaps?
                  */
                var @event = new AccountCreated(command.AccountNumber);
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);
                    _log.Debug($"Created account {command.AccountNumber}");
                });
                
            }
            else
            {
                _log.Warning(
                    $"You are trying to create {command.AccountNumber}, but has already been created. No action taken.");
            }
        }

       

        private void ApplyBusinessRules(IDomainCommand command)
        {
            Monitor();
            /**
			 * Here we can call Business Rules to validate and do whatever.
			 * Then, based on the outcome generated events.
			 * In this example, we are simply going to accept it and updated our state.
			 */
            BusinessRuleApplicationResult result = AccountBusinessRulesManager.ApplyBusinessRules(_accountState, command);
            _log.Info($"There were {result.GeneratedEvents.Count} events for {command} command. And it was {result.Success}");
            if (result.Success)
            {
                /* I may want to do old vs new state comparisons for other reasons
				 *  but ultimately we just update the state.. */
                var events = result.GeneratedEvents;
                foreach (var @event in events)
                {
                    Persist(@event, s =>
                    {
                        _log.Info($"Processing event {@event.ToString()} ");
                        _accountState = _accountState.ApplyEvent(@event);
                        ApplySnapShotStrategy();
                     });

                }
            }
        }
        //private void CustomPersistence()
        //{
        //    if (db == null)
        //    {
        //        db = new OneDatabasePerActor(_accountState.AccountNumber.Substring(0, 3));
        //    }
        //
        //    db.Snapshot(PersistenceId, typeof(AccountActor).ToString(), _accountState);
        //}
        /*Example of how snapshotting can be custom to the actor, in this case per 'Account' events*/
        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr != 0 && LastSequenceNr % TAKE_ACCOUNT_SNAPSHOT_AT == 0)
            {   
                Context.IncrementCounter("SnapShotTaken");
                //CustomPersistence();
                SaveSnapshot(_accountState);
            }
        }

        private void Monitor()
        {
            Context.IncrementMessagesReceived();
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
        }
        
        private void RecoveryCounter()
        {
            Context.IncrementCounter("AccountRecovery");
        }
        private void IncrementCounter(string counterName){
            Context.IncrementCounter(counterName);
        }
    }
}