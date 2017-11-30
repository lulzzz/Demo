﻿
namespace Loaner.api.Models
{
    using System.Collections.Generic;

    public class SupervisedAccounts 
    {
        public SupervisedAccounts()
        {
            Accounts = new Dictionary<string, string>();
        }
        public SupervisedAccounts(string message, Dictionary<string,string> accounts)
        {
            Message = message;
            Accounts = accounts;
        }

        public string Message
        {
            get;
             
        }

        public Dictionary<string, string> Accounts
        {
            get;
             
        }
    }

} 