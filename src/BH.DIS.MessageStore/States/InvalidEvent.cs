﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BH.DIS.MessageStore.States
{
    public class InvalidEvent
    {
        public string EventTypeId { get; set; }
        public string EventId { get; set; }
        public string EnqueueTime { get; set; }
    }
}
