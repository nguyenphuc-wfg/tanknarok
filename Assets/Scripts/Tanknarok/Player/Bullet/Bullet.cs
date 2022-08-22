using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace FishNetworking.Tanknarok
{
    /// <summary>
    /// The Bullet class simulates moving projectiles with gravity and applies area damage
    /// All motion is kinematic and is handled in FixedUpdateNetwork along with collision detection.
    ///
    /// Collision detection uses lag compensated hitboxes to allow server authoritative collision
    /// detection while still providing a WYSIWYG experience to players.
    ///
    /// Area damage is very simplistic and does not consider obstacles. It simply applies a linearly
    /// interpolated impulse and damage to objects within a certain radius. 
    ///
    /// Bullet motion is controlled by a speed, gravity and a time to live.
    ///
    /// Further, Bullet.cs uses predictive spawning to provide immediate feedback on the client
    /// firing the bullet even when this does not have state authority (hosted mode).
    /// </summary>
    
    public class Bullet : Projectile
    {
        public interface ITargetVisuals
        {
            void InitializeTargetMarker(Vector3 launchPos, Vector3 bulletVelocity, Bullet.BulletSettings bulletSettings);
            void Destroy();
        }

        [Header("Visuals")]
        [SerializeField] private Transform _bulletVisualParent;
        [SerializeField] ExplosionFX _explosionFX;

        [Header("Settings")]
        [SerializeField] private BulletSettings _bulletSettings;

        [Serializable]
        public class BulletSettings
        {
            public LayerMask hitMask;
            public float areaRadius;
            public float areaImpulse;
            public byte areaDamage;
            public float speed = 100;
            public float radius = 0.25f;
            public float gravity = -10f;
            public float timeToLive = 1.5f;
            public float timeToFade = 0.5f;
            public float ownerVelocityMultiplier = 1f;
        }
        private Vector3 mPrevPos;
        private bool destroyed;
        /// <summary>
        /// Because Bullet.cs uses predictive spawning, we have two different sets of properties:
        /// Networked and Predicted, hidden behind a common front that exposes the relevant value depending on the current state of the object.
        /// This allow us to use the same code in both the predicted and the confirmed state.
        /// </summary>

        // [Networked]
        // public TickTimer networkedLifeTimer { get; set; }
        // private TickTimer _predictedLifeTimer;
        // private TickTimer lifeTimer
        // {
        //     get => Object.IsPredictedSpawn ? _predictedLifeTimer : networkedLifeTimer;
        //     set { if (Object.IsPredictedSpawn) _predictedLifeTimer = value; else networkedLifeTimer = value; }
        // }
        //
        // [Networked]
        // public TickTimer networkedFadeTimer { get; set; }
        // private TickTimer _predictedFadeTimer;
        // private TickTimer fadeTimer
        // {
        //     get => Object.IsPredictedSpawn ? _predictedFadeTimer : networkedFadeTimer;
        //     set { if (Object.IsPredictedSpawn) _predictedFadeTimer = value; else networkedFadeTimer = value; }
        // }

        [SyncVar] public Vector3 networkedVelocity;
        private Vector3 _predictedVelocity;
        private float velocityOwner;
        public Vector3 velocity
        {
            get;
            set;
            // get => Object.IsPredictedSpawn ? _predictedVelocity : networkedVelocity;
            // set { if (Object.IsPredictedSpawn) _predictedVelocity = value; else networkedVelocity = value; }
        }
        //
        [SyncVar(OnChange = nameof(OnDestroyedChanged))]
        public bool networkedDestroyed;
        // private bool _predictedDestroyed;
        // private bool destroyed
        // {
        //     get;
        //     set;
        //     // get => Object.IsPredictedSpawn ? _predictedDestroyed : (bool)networkedDestroyed;
        //     // set { if (Object.IsPredictedSpawn) _predictedDestroyed = value; else networkedDestroyed = value; }
        // }
        //
        // private List<LagCompensatedHit> _areaHits = new List<LagCompensatedHit>();
        private ITargetVisuals _targetVisuals;

        private void Awake()
        {
            _targetVisuals = GetComponent<ITargetVisuals>();
        }
        
        // /// <summary>
        // /// PreSpawn is invoked directly when Spawn() is called, before any network state is shared, so this is where we initialize networked properties.
        // /// </summary>
        // /// <param name="ownervelocity"></param>
        public override void InitNetworkState(Vector3 ownervelocity)
        {
            // lifeTimer = TickTimer.CreateFromSeconds(Runner, _bulletSettings.timeToLive + _bulletSettings.timeToFade);
            // fadeTimer = TickTimer.CreateFromSeconds(Runner, _bulletSettings.timeToFade);
        
            // destroyed = false;
        
            Vector3 fwd = transform.forward.normalized;
            Vector3 vel = ownervelocity.normalized;
            vel.y = 0;
            fwd.y = 0;
            float multiplier = Mathf.Abs(Vector3.Dot(vel, fwd));
        
            // velocity = _bulletSettings.speed * transform.forward + ownervelocity * multiplier * _bulletSettings.ownerVelocityMultiplier;
        }
        

        public void Start()
        {
            base.OnStartClient();
            if (_explosionFX != null)
                _explosionFX.ResetExplosion();
            _bulletVisualParent.gameObject.SetActive(true);
        
            if (velocity.sqrMagnitude > 0)
                _bulletVisualParent.forward = velocity;
        
            _bulletVisualParent.forward = transform.forward;
        
            if (_targetVisuals != null)
                _targetVisuals.InitializeTargetMarker(transform.position, velocity, _bulletSettings);
        
            // We want bullet interpolation to use predicted data on all clients because we're moving them in FixedUpdateNetwork()
            // GetComponent<NetworkTransform>().InterpolationDataSource = InterpolationDataSources.Predicted;
        }

        private void OnDestroy()
        {
            // Explicitly destroy the target marker because it may not currently be a child of the bullet
            if (_targetVisuals != null)
                _targetVisuals.Destroy();
        }

        /// <summary>
        /// Simulate bullet movement and check for collision.
        /// This executes on all clients using the Velocity and last validated state to predict the correct state of the object
//         /// </summary>
         public void Update()
         {
             MoveBullet();
         }

         private void MoveBullet()
         {
             mPrevPos = transform.position;
             if (destroyed)
             {
                 return;
             }
             transform.Translate(Vector3.forward* _bulletSettings.speed * Time.deltaTime);
             transform.Translate(Vector3.up* _bulletSettings.gravity/5 *Time.deltaTime);
             // Transform xfrm = transform;
             // float dt = Time.deltaTime;
             // Vector3 vel = velocity;
             // float speed = vel.magnitude;
             // Vector3 pos = transform.position;
                
             if (!destroyed)
             {
                 // vel.y += dt * _bulletSettings.gravity;
                 RaycastHit hit;
                // We move the origin back from the actual position to make sure we can't shoot through things even if we start inside them
                 // Vector3 dir = vel.normalized;
                 // if (Physics.Raycast(pos - 0.5f * dir, dir, out hit, Mathf.Max(_bulletSettings.radius, speed * dt), _bulletSettings.hitMask.value))
                 // {
                 //     vel = HandleImpact(hit);
                 //     // pos = hit.point;
                 // }

                 if (Physics.Raycast(mPrevPos, (transform.position - mPrevPos).normalized, out hit, (transform.position - mPrevPos).magnitude, _bulletSettings.hitMask.value))
                 {
                     HandleImpact(hit);
                 }

             }

             // If the bullet is destroyed, we stop the movement so we don't get a flying explosion


             // velocity = vel;
             // pos += dt * velocity;

             // xfrm.position = pos;
             // if (vel.sqrMagnitude > 0)
             //     _bulletVisualParent.forward = vel.normalized;

         }
//
//         /// <summary>
//         /// Bullets will detonate when they expire or on impact.
//         /// After detonating, the mesh will disappear and it will no longer collide.
//         /// If specified, an impact fx may play and area damage may be applied.
//         /// </summary>
         private void Detonate(Vector3 hitPoint)
         {
             if (destroyed)
                 return;
             // Mark the bullet as destroyed.
             // This will trigger the OnDestroyedChanged callback which makes sure the explosion triggers correctly on all clients.
             // Using an OnChange callback instead of an RPC further ensures that we don't trigger the explosion in a different frame from
             // when the bullet stops moving (That would lead to moving explosions, or frozen bullets)
             destroyed = true;
             OnDestroyedChanged();
             if (_bulletSettings.areaRadius > 0)
             {
                 ApplyAreaDamage(hitPoint);
             }
         }
//
         public void OnDestroyedChanged(bool prev, bool next, bool asServer)
         {
             Debug.Log("On destroy bullet 12323");
             OnDestroyedChanged();
         }

         private void OnDestroyedChanged()
         {
             if (destroyed)
             {
                 if (_explosionFX != null)
                 {
                     transform.up = Vector3.up;
                     _explosionFX.PlayExplosion();
                 }
                 _bulletVisualParent.gameObject.SetActive(false);
             }
         }

         private void ApplyAreaDamage(Vector3 hitPoint)
         {
             Collider[] hitColliders = Physics.OverlapSphere(hitPoint, _bulletSettings.areaRadius,_bulletSettings.hitMask);
             foreach (var hitCollider in hitColliders)
             {
                 GameObject other = hitCollider.gameObject;
                 if (other)
                 {
                     ICanTakeDamage target = other.GetComponent<ICanTakeDamage>();
                     if (target != null)
                     {
                         Vector3 impulse = other.transform.position - hitPoint;
                         float l = Mathf.Clamp(_bulletSettings.areaRadius - impulse.magnitude, 0, _bulletSettings.areaRadius);
                         impulse = _bulletSettings.areaImpulse * l * impulse.normalized;
                         target.ApplyDamage(impulse, _bulletSettings.areaDamage, base.Owner);
                     }
                 }
             }
             // if (cnt > 0)
             // {
             //     for (int i = 0; i < cnt; i++)
             //     {
             //         GameObject other = _areaHits[i].GameObject;
             //         if (other)
             //         {
             //             ICanTakeDamage target = other.GetComponent<ICanTakeDamage>();
             //             if (target != null)
             //             {
             //                 Vector3 impulse = other.transform.position - hitPoint;
             //                 float l = Mathf.Clamp(_bulletSettings.areaRadius - impulse.magnitude, 0, _bulletSettings.areaRadius);
             //                 impulse = _bulletSettings.areaImpulse * l * impulse.normalized;
             //                 target.ApplyDamage(impulse, _bulletSettings.areaDamage, Object.InputAuthority);
             //             }
             //         }
             //     }
             // }
         }

         private Vector3 HandleImpact(RaycastHit hit)
         {
             // if (hit.collider != null)
             // {
             //     NetworkObject netobj = hit.Hitbox.Root.Object;
             //     if (netobj != null && Object != null && netobj.InputAuthority == Object.InputAuthority)
             //         return velocity; // Don't let us hit ourselves - this is esp. important with lag compensation since, if we move backwards, we're very likely to hit our own ghost from a previous frame.
             // }
             //
             Detonate(hit.point);
             //
             return velocity;
         }

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(transform.position, _bulletSettings.radius);
			if (_bulletSettings.areaRadius > 0)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(transform.position, _bulletSettings.areaRadius);
			}
		}
#endif
    }
}