using FishNet;
using FishNet.Connection;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    public interface ICanTakeDamage
    {
        void ApplyDamage(Vector3 impulse, byte damage, NetworkConnection source);
    }
}