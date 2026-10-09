using UnityEngine;

namespace CannibalRally
{
    public sealed class BuggyVehicle : MonoBehaviour
    {
        public RallyState State;
        public RallyConfig Config;
        public Rigidbody Body;
        public bool Driven;
        private WheelCollider[] wheels;
        private Transform[] visuals;
        private float throttle, steer;
        private bool brake;
        private float lastImpact = -10;

        public void Configure(Rigidbody body, WheelCollider[] suspension, Transform[] meshes, RallyConfig config, RallyState state)
        { Body = body; wheels = suspension; visuals = meshes; Config = config; State = state; }

        public void SetInput(float acceleration, float steering, bool braking)
        { throttle = acceleration; steer = steering; brake = braking; }

        private void FixedUpdate()
        {
            if (State == null) return;
            bool active = Driven && Application.isFocused && Time.timeScale > 0;
            float gas = active ? throttle : 0;
            bool powered = active && State.CanDrive && Body.velocity.magnitude < Config.MaxSpeed;
            for (int i = 0; i < wheels.Length; ++i)
            {
                wheels[i].motorTorque = powered ? gas * Config.WheelTorque * (0.3f + 0.7f * State.Integrity / 100) : 0;
                wheels[i].brakeTorque = !active || brake || State.Integrity <= 0 ? Config.BrakeTorque : 0;
                wheels[i].steerAngle = i < 2 && active ? steer * Mathf.Lerp(32, 12, Body.velocity.magnitude / Config.MaxSpeed) : 0;
            }
            if (active) State.Consume(Time.fixedDeltaTime, gas, Config);
        }

        private void LateUpdate()
        {
            if (wheels == null) return;
            for (int i = 0; i < wheels.Length; ++i)
            {
                Vector3 position; Quaternion rotation;
                wheels[i].GetWorldPose(out position, out rotation);
                visuals[i].position = position; visuals[i].rotation = rotation;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (State == null || Time.time - lastImpact < 0.3f) return;
            float impact = collision.relativeVelocity.magnitude;
            if (impact <= 4) return;
            lastImpact = Time.time;
            State.Damage(Mathf.Min(100, (impact - 4) * Config.CollisionDamageScale));
        }

        public void Capture()
        {
            State.X = transform.position.x; State.Y = transform.position.y;
            State.Z = transform.position.z; State.Yaw = transform.eulerAngles.y;
        }
    }
}
