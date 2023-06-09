﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BH.DIS.Core.Events
{
    public class EventType<T_Event> : EventType, IEventType
        where T_Event : IEvent
    {
        public EventType() : base(typeof(T_Event)) { }
    }

    public class EventType : IEventType
    {
        private readonly Type _type;

        public EventType(Type type)
        {
            _type = type;
        }

        public string Id => _type.Name;

        public string Name => _type.Name;

        public string Namespace => _type.Namespace;

        public string Description => _type.GetCustomAttribute<DescriptionAttribute>()?.Description;

        public IEnumerable<IProperty> Properties =>
            _type.GetProperties()
            .Select(p => new Property(p));

        public override bool Equals(object obj) =>
            obj is EventType et &&
            et._type == _type;

        public override int GetHashCode() =>
            _type.GetHashCode();

        public Type GetEventClassType() => _type;

        public IEvent GetEventExample() =>
                (IEvent) _type.GetFields()?.FirstOrDefault()?.GetValue(null);
          
    }
}