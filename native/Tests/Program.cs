using System;
using System.IO;
using CannibalRally;

internal static class Program
{
    private static int count;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); ++count; }
    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, message);
    }
    private static void Main()
    {
        RallyConfig config = new RallyConfig(); config.Validate();
        RallyState state = new RallyState();
        state.Fuel = 0.001f; state.Consume(1, 1, config);
        Check(state.Fuel == 0 && !state.CanDrive, "Empty tank must stop propulsion.");
        state.Integrity = 100; state.Damage(200);
        Check(state.Integrity == 0 && !state.CanDrive, "Destroyed vehicle cannot drive.");
        Check(state.Collect(0) && !state.Collect(0), "Pickup cannot be duplicated.");
        Check(!state.Collect(-1) && !state.Collect(12), "Invalid pickup IDs rejected.");
        Check(!state.Refuel(2, config) && state.ReserveFuel == 5, "Moving service must not debit resources.");
        Check(state.Refuel(0, config) && state.Fuel == 5 && state.ReserveFuel == 0, "Fuel transfer conserves litres.");
        state.Fuel = 29; state.ReserveFuel = 5; state.Refuel(0, config);
        Check(state.Fuel == 30 && state.ReserveFuel == 4, "Tank overflow must stay in reserve.");
        Check(!state.Repair(0), "Repair needs a part.");
        state.Collect(1); Check(!state.Repair(1) && state.Parts == 1, "Moving repair cannot debit part.");
        Check(state.Repair(0) && state.Integrity == 25 && state.Parts == 0, "Repair consumes exactly one part.");
        state.Parts = 1; state.Integrity = 100;
        Check(!state.Repair(0) && state.Parts == 1, "Healthy vehicle must not consume a part.");
        state.Fuel = 8; state.Consume(0, 1, config);
        Check(state.Fuel == 8, "Paused time must not burn fuel.");
        Reject(delegate { state.Fuel = float.NaN; state.Validate(config); }, "NaN save rejected.");
        state.Fuel = 8; state.Schema = 2;
        Reject(delegate { state.Validate(config); }, "Future schema rejected."); state.Schema = 1;
        state.Collected.Add(0);
        Reject(delegate { state.Validate(config); }, "Duplicate pickup ledger rejected."); state.Collected.RemoveAt(state.Collected.Count - 1);
        config.Profile = "../escape";
        Reject(delegate { config.Validate(); }, "Profile traversal rejected."); config.Profile = "test-save";
        config.WheelTorque = float.PositiveInfinity;
        Reject(delegate { config.Validate(); }, "Non-finite config rejected."); config.WheelTorque = 650;
        string folder = Path.Combine(Path.GetTempPath(), "cannibal-rally-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "save.xml");
            state.X = 123; state.Z = -321; state.Integrity = 63;
            SidecarStore.Write(path, state);
            RallyState loaded = SidecarStore.Load(path, config);
            Check(loaded.X == 123 && loaded.Z == -321 && loaded.Integrity == 63 && loaded.Collected.Count == 2,
                  "Sidecar roundtrip preserves progress and pickup ledger.");
            state.Integrity = 51; SidecarStore.Write(path, state);
            Check(SidecarStore.Load(path, config).Integrity == 51 && SidecarStore.Load(path + ".bak", config).Integrity == 63,
                  "Atomic replacement retains previous save.");
            Check(!File.Exists(path + ".tmp"), "Temporary save must be cleaned up.");
            File.WriteAllText(path, "<!DOCTYPE RallyState [<!ENTITY x SYSTEM 'file:///nonexistent'>]><RallyState><Fuel>&x;</Fuel></RallyState>");
            bool blocked = false;
            try { SidecarStore.Load(path, config); } catch (Exception) { blocked = true; }
            Check(blocked, "External entities/DTD must be blocked.");
            Check(SidecarStore.Load(path + ".bak", config).Integrity == 63, "Malformed primary must leave backup intact.");
        }
        finally { Directory.Delete(folder, true); }
        Console.WriteLine("PASS: " + count + " assertions against actual C# core and XML store. Unity runtime not tested.");
    }
}
