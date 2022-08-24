using System;
using UnityEngine;

namespace FishNetworking.Event
{
    [CreateAssetMenu(fileName="GameEvent", menuName = "Event/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        private Action _actionEvent;

        public void Excute()
        {
            _actionEvent?.Invoke();
        }

        public void Sub(Action callback)
        {
            _actionEvent -= callback;
            _actionEvent += callback;
        }

        public void UnSub(Action callback)
        {
            _actionEvent -= callback;
        }
    }
}