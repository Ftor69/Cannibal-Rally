using System.Collections.Generic;
using TheForest.Utils;
using UnityEngine;

namespace CannibalRally
{
    // Only published LocalPlayer members plus Unity APIs are used here.
    // Native health/AI remain active; this is not an invulnerability mode.
    public sealed class DriverSession
    {
        private BuggyVehicle vehicle;
        private Transform player;
        private Rigidbody body;
        private Camera camera;
        private Behaviour[] controls;
        private bool[] enabled;
        private bool kinematic, gravity;
        private Vector3 cameraLocalPosition;
        private Quaternion cameraLocalRotation;
        private float health;
        private readonly List<CollisionPair> pairs = new List<CollisionPair>();
        private sealed class CollisionPair { public Collider A, B; public bool Ignored; }
        public bool Mounted { get { return vehicle != null; } }

        public bool Enter(BuggyVehicle target)
        {
            if (Mounted || LocalPlayer.Transform == null || LocalPlayer.FpCharacter == null ||
                LocalPlayer.Stats == null || LocalPlayer.Stats.Health <= 0 || Camera.main == null ||
                LocalPlayer.FpCharacter.Locked || target.Body.velocity.magnitude > 0.5f ||
                Vector3.Distance(LocalPlayer.Transform.position, target.transform.position) > 4) return false;
            body = LocalPlayer.FpCharacter.GetComponent<Rigidbody>();
            if (body == null) return false;
            player = LocalPlayer.Transform; camera = Camera.main;
            controls = new Behaviour[] { LocalPlayer.FpCharacter, LocalPlayer.CamFollowHead,
                                        LocalPlayer.CamRotator, LocalPlayer.MainRotator };
            enabled = new bool[controls.Length];
            cameraLocalPosition = camera.transform.localPosition;
            cameraLocalRotation = camera.transform.localRotation;
            kinematic = body.isKinematic; gravity = body.useGravity;
            vehicle = target; // Set before mutation so cleanup can restore a partial entry.
            for (int i = 0; i < controls.Length; ++i)
            {
                if (controls[i] == null) continue;
                enabled[i] = controls[i].enabled;
            }
            for (int i = 0; i < controls.Length; ++i)
                if (controls[i] != null) controls[i].enabled = false;
            body.velocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.isKinematic = true; body.useGravity = false;
            foreach (Collider a in player.GetComponentsInChildren<Collider>())
                foreach (Collider b in vehicle.GetComponentsInChildren<Collider>())
                {
                    CollisionPair pair = new CollisionPair(); pair.A = a; pair.B = b;
                    pair.Ignored = Physics.GetIgnoreCollision(a, b); pairs.Add(pair);
                    Physics.IgnoreCollision(a, b, true);
                }
            health = LocalPlayer.Stats.Health;
            target.Driven = true;
            return true;
        }

        public bool Exit()
        {
            if (!Mounted || vehicle.Body.velocity.magnitude > 0.5f) return false;
            Vector3 position;
            if (!TryExitPosition(out position)) return false;
            Restore(position, true); return true;
        }

        private bool TryExitPosition(out Vector3 position)
        {
            foreach (Vector3 direction in new Vector3[] { vehicle.transform.right, -vehicle.transform.right,
                                                         -vehicle.transform.forward })
            {
                Vector3 start = vehicle.transform.position + direction * 2.5f + Vector3.up * 3;
                RaycastHit hit;
                if (!Physics.Raycast(start, Vector3.down, out hit, 7, ~0, QueryTriggerInteraction.Ignore) ||
                    Vector3.Dot(hit.normal, Vector3.up) < 0.7f) continue;
                Vector3 foot = hit.point + Vector3.up * 0.15f;
                bool blocked = false;
                foreach (Collider obstacle in Physics.OverlapCapsule(foot + Vector3.up * 0.4f,
                         foot + Vector3.up * 1.5f, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (obstacle.transform.IsChildOf(player)) continue;
                    blocked = true; break;
                }
                if (blocked) continue;
                // LocalPlayer.Transform's pivot must be verified against the installed game's player rig.
                position = foot + Vector3.up; return true;
            }
            position = Vector3.zero; return false;
        }

        public void Tick()
        {
            if (!Mounted) return;
            if (player == null || body == null || LocalPlayer.Stats == null || LocalPlayer.Stats.Health <= 0)
            { Release(); return; }
            // Keep the native player target with the buggy. No replacement enemy AI is fabricated.
            player.position = vehicle.transform.TransformPoint(new Vector3(-0.35f, 1, -0.2f));
            player.rotation = Quaternion.Euler(0, vehicle.transform.eulerAngles.y, 0);
            float next = LocalPlayer.Stats.Health;
            if (next < health) vehicle.State.Damage(Mathf.Min(100, (health - next) * vehicle.Config.PassengerDamageScale));
            health = next;
            if (camera != null)
            {
                Vector3 focus = vehicle.transform.position + Vector3.up;
                Vector3 desired = focus - vehicle.transform.forward * 6 + Vector3.up * 2;
                RaycastHit hit;
                if (Physics.Linecast(focus, desired, out hit, ~0, QueryTriggerInteraction.Ignore) &&
                    !hit.transform.IsChildOf(vehicle.transform) && !hit.transform.IsChildOf(player))
                    desired = hit.point + hit.normal * 0.25f;
                camera.transform.position = desired;
                camera.transform.rotation = Quaternion.LookRotation(focus - desired, Vector3.up);
            }
        }

        public void Release()
        {
            if (!Mounted && controls == null) return;
            Vector3 position = Vector3.zero;
            bool move = vehicle != null && player != null && TryExitPosition(out position);
            if (!move) position = player != null ? player.position : Vector3.zero;
            Restore(position, move);
        }

        private void Restore(Vector3 position, bool move)
        {
            if (vehicle != null) { vehicle.Driven = false; vehicle.SetInput(0, 0, true); }
            foreach (CollisionPair pair in pairs)
                if (pair.A != null && pair.B != null) Physics.IgnoreCollision(pair.A, pair.B, pair.Ignored);
            pairs.Clear();
            if (player != null && move) player.position = position;
            if (body != null) { body.isKinematic = kinematic; body.useGravity = gravity; }
            if (camera != null)
            { camera.transform.localPosition = cameraLocalPosition; camera.transform.localRotation = cameraLocalRotation; }
            if (controls != null)
                for (int i = 0; i < controls.Length; ++i)
                    if (controls[i] != null) controls[i].enabled = enabled[i];
            vehicle = null; controls = null; player = null; body = null; camera = null;
        }
    }
}
