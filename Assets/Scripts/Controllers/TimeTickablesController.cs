using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace YellowOrphan.Controllers
{
    public class TimeTickablesController : ITickable, ITimeTickable
    {
        private readonly List<TimeTickable> _timeTickables = new List<TimeTickable>();

        public void Tick()
            => _timeTickables.ForEach(x => x.TryCall());

        public void AddTickable(float delay, Action action)
            => _timeTickables.Add(new TimeTickable(action, delay));
    }

    public class TimeTickable
    {
        private readonly Action _action;

        private readonly float _delay;
        private float _lastCall;

        private static float CurrentTime => Time.time;

        public TimeTickable(Action action, float delay)
        {
            _action = action;
            _delay = delay;
            _lastCall = CurrentTime;
        }

        public void TryCall()
        {
            if (_lastCall + _delay >= CurrentTime)
                return;
            _action.Invoke();
            _lastCall += _delay;
        }
    }

    public interface ITimeTickable
    {
        public void AddTickable(float delay, Action action);
    }
}