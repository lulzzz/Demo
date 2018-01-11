﻿namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class Bucket 
    {
       
        public Bucket(string bucket, double balance)
        {
            Name = bucket;
            Amount = balance;
        }

        
        public string Name { get; }
        public double Amount { get; set; }
    }
}