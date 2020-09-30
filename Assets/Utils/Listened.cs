using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


namespace TeamBlack.Util
{
    [Serializable]
    public class Listened<T>
    {
        public static implicit operator T(Listened<T> l) => l._value;
        public static explicit operator Listened<T>(T t) => new Listened<T>(t);
    
        [SerializeField] private T _value;
        private Action _listener;

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _listener();
            }
        }

        public Listened(T t)
        {
            _value = t;
            _listener = () => { };
        }
    
        public void Listen(Action onChange)
        {
            _listener += onChange;
        }
        public void UnListen(Action unlisten) 
        {
            _listener -= unlisten;
        }

        public override string ToString() {
            return _value.ToString();
        }
    }
}
