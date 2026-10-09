using System;
using System.Collections.Generic;

namespace CannibalRally
{
    // Shared verbatim by the old Mono mod and the .NET test runner.
    [Serializable]
    public sealed class RallyState
    {
        public int Schema = 1;
        public float Fuel = 8;
        public float Integrity = 100;
        public float ReserveFuel;
        public int Parts;
        public float X, Y, Z, Yaw;
        public float OriginX, OriginY, OriginZ;
        public List<int> Collected = new List<int>();

        public void Validate(RallyConfig config)
        {
            if (Schema != 1) throw new InvalidOperationException("Unsupported save schema.");
            Fuel = Bound(Fuel, 0, config.TankLitres);
            Integrity = Bound(Integrity, 0, 100);
            ReserveFuel = Bound(ReserveFuel, 0, 100);
            Parts = Math.Max(0, Math.Min(Parts, 100));
            X = Bound(X, -100000, 100000); Y = Bound(Y, -10000, 10000);
            Z = Bound(Z, -100000, 100000); Yaw = Bound(Yaw, -360, 360);
            OriginX = Bound(OriginX, -100000, 100000);
            OriginY = Bound(OriginY, -10000, 10000);
            OriginZ = Bound(OriginZ, -100000, 100000);
            if (Collected == null || Collected.Count > 12)
                throw new InvalidOperationException("Invalid pickup ledger.");
            HashSet<int> ids = new HashSet<int>();
            foreach (int id in Collected)
                if (id < 0 || id >= 12 || !ids.Add(id))
                    throw new InvalidOperationException("Invalid pickup ID.");
        }

        internal static float Bound(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new InvalidOperationException("Non-finite value.");
            return Math.Max(min, Math.Min(value, max));
        }

        public bool CanDrive { get { return Fuel > 0 && Integrity > 0; } }

        public void Consume(float seconds, float throttle, RallyConfig config)
        {
            if (!CanDrive || seconds <= 0) return;
            seconds = Bound(seconds, 0, 1);
            throttle = Bound(throttle, -1, 1);
            Fuel = Math.Max(0, Fuel - seconds *
                (config.IdleLitresPerSecond + Math.Abs(throttle) * config.LoadLitresPerSecond));
        }

        public void Damage(float amount)
        {
            Integrity = Math.Max(0, Integrity - Bound(amount, 0, 100));
        }

        public bool Refuel(float speed, RallyConfig config)
        {
            if (Math.Abs(speed) > 0.5f || ReserveFuel <= 0 || Fuel >= config.TankLitres) return false;
            float transfer = Math.Min(ReserveFuel, config.TankLitres - Fuel);
            Fuel += transfer; ReserveFuel -= transfer;
            return true;
        }

        public bool Repair(float speed)
        {
            if (Math.Abs(speed) > 0.5f || Parts <= 0 || Integrity >= 100) return false;
            --Parts; Integrity = Math.Min(100, Integrity + 25);
            return true;
        }

        public bool Collect(int id)
        {
            if (id < 0 || id >= 12 || Collected.Contains(id)) return false;
            if (id % 2 == 0 && ReserveFuel > 95) return false;
            if (id % 2 != 0 && Parts >= 100) return false;
            Collected.Add(id);
            if (id % 2 == 0) ReserveFuel += 5; else ++Parts;
            return true;
        }
    }

    [Serializable]
    public sealed class RallyConfig
    {
        // A user-chosen sidecar profile, never inferred from a native save slot.
        public string Profile = "sandbox";
        public float TankLitres = 30;
        public float IdleLitresPerSecond = 0.002f;
        public float LoadLitresPerSecond = 0.015f;
        public float WheelTorque = 650;
        public float BrakeTorque = 1800;
        public float MaxSpeed = 28; // metres/second
        public float CollisionDamageScale = 2;
        public float PassengerDamageScale = 2;

        public void Validate()
        {
            if (String.IsNullOrEmpty(Profile) || Profile.Length > 40)
                throw new InvalidOperationException("Invalid profile.");
            foreach (char c in Profile)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                      (c >= '0' && c <= '9') || c == '-' || c == '_'))
                    throw new InvalidOperationException("Profile must contain only ASCII letters, digits, - or _.");
            TankLitres = RallyState.Bound(TankLitres, 5, 100);
            IdleLitresPerSecond = RallyState.Bound(IdleLitresPerSecond, 0, 0.1f);
            LoadLitresPerSecond = RallyState.Bound(LoadLitresPerSecond, 0.001f, 1);
            WheelTorque = RallyState.Bound(WheelTorque, 100, 2000);
            BrakeTorque = RallyState.Bound(BrakeTorque, 100, 5000);
            MaxSpeed = RallyState.Bound(MaxSpeed, 5, 45);
            CollisionDamageScale = RallyState.Bound(CollisionDamageScale, 0.1f, 10);
            PassengerDamageScale = RallyState.Bound(PassengerDamageScale, 0.1f, 10);
        }
    }
}
