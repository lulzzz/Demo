﻿using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Confluent.Kafka.Serialization;
using Confluent.Kafka;
using Newtonsoft.Json; 
using Akka.Monitoring;

namespace Loaner.KafkaProducer
{
    #region Command classes
    // Command to flush the producer

    #endregion

    class KafkaPublisherFlushActor : ReceiveActor
    {
        Producer<string, string> producer;


        public KafkaPublisherFlushActor(Producer<string, string> producer)
        {
            this.producer = producer;

            // Commands
            Receive<Flush>(cmd => FlushProducer());
        }


        // Send a message that has not been converted to json
        private void FlushProducer()
        {
            var queueSize = producer.Flush(10);
            Context.Gauge("QueueOfMsgsToSend", queueSize);
        }
    }
}









