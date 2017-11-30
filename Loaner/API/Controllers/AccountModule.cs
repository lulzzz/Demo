﻿
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Util.Internal;
using Newtonsoft.Json;

namespace Loaner.api.Controllers
{
    using Akka.Actor;
    using Models;
    using BoundedContexts.MaintenanceBilling.Commands;
    using BoundedContexts.MaintenanceBilling.Events;
    using BoundedContexts.MaintenanceBilling.Models;
    using Nancy;
    using Nancy.ModelBinding;
    using System;
    using System.Threading.Tasks;
    using static ActorManagement.LoanerActors;
    
    public class AccountModule : NancyModule
    {
        public AccountModule() : base("/api/account")
        {
 

            Get("/{actorName}", async args =>
             {
                 try
                 {
                     var system = DemoActorSystem
                         .ActorSelection($"/user/demoSupervisor/*/{args.actorName}")
                         .ResolveOne(TimeSpan.FromSeconds(3));
                     if (system.Exception != null)
                     {
                         throw system.Exception;
                     }
                     var response = await Task.Run(
                         () => system.Result.Ask<MyAccountStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(1))
                         );
                     return Response.AsJson(response.Message);
                 }
                 catch (ActorNotFoundException)
                 {
                     return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                 }
                 catch (Exception e)
                 {
                     return new AccountStateViewModel($"{args.actorName} {e.Message}");

                 }
             });

            Get("/{actorName}/info", async args =>
            {
                try
                {
                    string account = args.actorName;
                    string path = $@"/user/demoSupervisor/*/{account}";
                    var system = DemoActorSystem
                        .ActorSelection(path)
                        .ResolveOne(TimeSpan.FromSeconds(3)).Result;


                    if (system.IsNobody())
                    {
                        throw new ActorNotFoundException();
                    }
                    var response = await Task.Run(
                        () => system.Ask<MyAccountStatus>(new TellMeYourInfo(), TimeSpan.FromSeconds(3))
                    );
                    response.AccountState.AuditLog.Sort((x, y) => x.EventDate >= y.EventDate ? 1 : -1);
                    return Response.AsJson(new AccountStateViewModel(response.AccountState));
                }
                catch (ActorNotFoundException)
                {
                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                }
                catch (Exception e)
                {
                    return new AccountStateViewModel($"{args.actorName} {e.Message}");

                }
            });
            Get("/{actorName}/assessment", args =>
            {
                InvoiceLineItem[] lineItems = new InvoiceLineItem[]
                {
                    new InvoiceLineItem(new Tax(), 0,0,0),
                    new InvoiceLineItem(new Dues(), 0,0,0),
                    new InvoiceLineItem(new Reserve(), 0,0,0)
                };
                
                
                return lineItems;
            });

            Post("/{actorName}/assessment", async args =>
            {
                string account = args.actorName;
                //var assessment = this.Bind<InvoiceLineItem>();
                var reader = new StreamReader(this.Request.Body);
                string text = reader.ReadToEnd();
                InvoiceLineItem assessment = JsonConvert.DeserializeObject<InvoiceLineItem>(text);
                
                  Console.WriteLine($"assessment: {assessment.Item.Name} \t");
               
                List<InvoiceLineItem> liofInvoices = new List<InvoiceLineItem>();
                liofInvoices.Add(assessment);
                try
                {
                     
                    var domanCommand = new BillingAssessment(account, liofInvoices);
                    var system = DemoActorSystem
                        .ActorSelection($"/user/demoSupervisor/*/{account}")
                        .ResolveOne(TimeSpan.FromSeconds(3));
                    if (system.Exception != null)
                    {
                        throw system.Exception;
                    }
                    var response = await Task.Run(
                        () => system.Result.Ask<MyAccountStatus>(domanCommand, TimeSpan.FromSeconds(30))
                    );
                    return Response.AsJson(response);
                }
                catch (ActorNotFoundException)
                {
                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                }
                catch (Exception e)
                {
                    return new AccountStateViewModel($"{args.actorName} {e.Message}");

                }
            });
            
        }
    }
}
