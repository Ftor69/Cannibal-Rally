using System;
using System.Collections.Generic;
using System.IO;
using ModAPI.Attributes;
using TheForest.Utils;
using UnityEngine;
using Input = UnityEngine.Input;

namespace CannibalRally
{
    public sealed class RallyMod : MonoBehaviour
    {
        private BuggyVehicle buggy;
        private RallyConfig config;
        private RallyState state;
        private readonly DriverSession driver = new DriverSession();
        private readonly Dictionary<int, GameObject> pickups = new Dictionary<int, GameObject>();
        private readonly HashSet<Material> materials = new HashSet<Material>();
        private string message = "EXPERIMENTAL ModAPI source: F6 initialize in a loaded single-player world.";
        private string savePath;
        private bool fault;

        [ExecuteOnGameStart]
        private static void Initialize()
        {
            if (UnityEngine.Object.FindObjectOfType(typeof(RallyMod)) == null)
                new GameObject("CannibalRally.Runtime").AddComponent<RallyMod>();
        }

        private void Update()
        {
            try
            {
                if (BoltNetwork.isRunning)
                {
                    driver.Release(); Cleanup();
                    message = "Disabled: Cannibal Rally currently supports single-player only.";
                    return;
                }
                if (LocalPlayer.Transform == null || LocalPlayer.FpCharacter == null || LocalPlayer.Stats == null)
                { driver.Release(); Cleanup(); return; }
                if (fault || !Application.isFocused || Time.timeScale <= 0 || LocalPlayer.FpCharacter.Locked)
                { if (buggy != null) buggy.SetInput(0, 0, true); return; }
                if (Input.GetKeyDown(KeyCode.F6) && buggy == null) Spawn();
                if (buggy == null) return;
                if (Input.GetKeyDown(KeyCode.F7))
                    message = driver.Mounted ? (driver.Exit() ? "Exited." : "Stop and find a clear exit.") :
                              (driver.Enter(buggy) ? "Driving. WASD, Space brake; F7 exit." : "Stand beside the stopped buggy.");
                float gas = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                float steer = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                buggy.SetInput(gas, steer, Input.GetKey(KeyCode.Space));
                bool near = Vector3.Distance(LocalPlayer.Transform.position, buggy.transform.position) <= 4;
                if (near && !driver.Mounted)
                {
                    if (Input.GetKeyDown(KeyCode.F9)) message = state.Repair(buggy.Body.velocity.magnitude) ? "Repaired +25." : "Need parts, damage and a stopped buggy.";
                    if (Input.GetKeyDown(KeyCode.F10)) message = state.Refuel(buggy.Body.velocity.magnitude, config) ? "Refuelled." : "Need reserve fuel, tank space and a stopped buggy.";
                }
                if (Input.GetKeyDown(KeyCode.F5) && !driver.Mounted) Collect();
                if (Input.GetKeyDown(KeyCode.F8))
                {
                    if (driver.Mounted || buggy.Body.velocity.magnitude > 0.5f) message = "Park and exit before saving.";
                    else
                    {
                        buggy.Capture(); state.Validate(config); SidecarStore.Write(savePath, state);
                        message = "Sidecar saved: " + config.Profile + " (native game must be saved separately).";
                    }
                }
            }
            catch (Exception error)
            {
                driver.Release(); fault = true;
                message = "Stopped after error; inspect Unity player log. No automatic save overwrite.";
                UnityEngine.Debug.LogError("CannibalRally: " + error);
            }
        }

        private void LateUpdate()
        {
            try { driver.Tick(); }
            catch (Exception error) { driver.Release(); fault = true; UnityEngine.Debug.LogError(error); }
        }

        private void Spawn()
        {
            string directory = Path.Combine(Application.persistentDataPath, "CannibalRally");
            string configPath = Path.Combine(directory, "config.xml");
            config = File.Exists(configPath) ? SidecarStore.Read<RallyConfig>(configPath) : new RallyConfig();
            config.Validate();
            if (!File.Exists(configPath)) SidecarStore.Write(configPath, config);
            savePath = Path.Combine(directory, config.Profile + ".xml");
            bool resume = File.Exists(savePath);
            state = resume ? SidecarStore.Load(savePath, config) : new RallyState();
            Vector3 desired = resume ? new Vector3(state.X, state.Y, state.Z) :
                LocalPlayer.Transform.position + LocalPlayer.Transform.forward * 5;
            Vector3 position;
            if (!GroundPosition(desired, out position))
            { message = "No safe ground at spawn/save position. Move to open terrain or use another profile."; return; }
            if (!resume)
            { state.OriginX = position.x; state.OriginY = position.y; state.OriginZ = position.z; }
            buggy = BuggyFactory.Create(position, Quaternion.Euler(0, resume ? state.Yaw : LocalPlayer.Transform.eulerAngles.y, 0), config, state).GetComponent<BuggyVehicle>();
            TrackMaterials(buggy.gameObject);
            CreatePickups();
            message = "F7 enter; F5 collect orange fuel / grey parts; F9 repair; F10 refuel; F8 sidecar save.";
        }

        private bool GroundPosition(Vector3 desired, out Vector3 position)
        {
            RaycastHit[] hits = Physics.RaycastAll(desired + Vector3.up * 4, Vector3.down, 12, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, delegate(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); });
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform.IsChildOf(LocalPlayer.Transform) ||
                    (buggy != null && hit.transform.IsChildOf(buggy.transform)) ||
                    Vector3.Dot(hit.normal, Vector3.up) < 0.85f) continue;
                Vector3 candidate = hit.point + Vector3.up * 0.85f;
                if (Physics.CheckBox(candidate, new Vector3(1.1f, 0.6f, 1.7f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)) continue;
                position = candidate; return true;
            }
            position = Vector3.zero; return false;
        }

        private void CreatePickups()
        {
            Vector3 origin = new Vector3(state.OriginX, state.OriginY, state.OriginZ);
            Material fuel = BuggyFactory.Material(new Color(1, 0.45f, 0));
            Material parts = BuggyFactory.Material(Color.gray); materials.Add(fuel); materials.Add(parts);
            for (int id = 0; id < 12; ++id)
            {
                if (state.Collected.Contains(id)) continue;
                float angle = id * Mathf.PI * 2 / 12;
                float radius = 18 + id % 3 * 8;
                Vector3 desired = origin + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Vector3 position;
                if (!GroundPosition(desired, out position)) continue;
                GameObject pickup = new GameObject(id % 2 == 0 ? "CannibalRally.Fuel" : "CannibalRally.Parts");
                pickup.transform.position = position - Vector3.up * 0.5f;
                BuggyFactory.Part(pickup, PrimitiveType.Cube, Vector3.zero, new Vector3(0.5f, 0.6f, 0.35f), id % 2 == 0 ? fuel : parts);
                pickups.Add(id, pickup);
            }
        }

        private void Collect()
        {
            int nearest = -1; float range = 3;
            foreach (KeyValuePair<int, GameObject> item in pickups)
            {
                if (item.Value == null) continue;
                float distance = Vector3.Distance(LocalPlayer.Transform.position, item.Value.transform.position);
                if (distance < range) { nearest = item.Key; range = distance; }
            }
            if (nearest >= 0 && state.Collect(nearest))
            {
                Destroy(pickups[nearest]); pickups.Remove(nearest);
                message = nearest % 2 == 0 ? "Collected 5 litres reserve fuel." : "Collected one repair part.";
            }
            else message = "Stand within 3 m of an original mod supply crate.";
        }

        private void TrackMaterials(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 750, 110), "Cannibal Rally — EXPERIMENTAL / ModAPI / Melty unverified");
            GUI.Label(new Rect(22, 40, 720, 30), message);
            if (buggy != null)
                GUI.Label(new Rect(22, 72, 720, 30), String.Format("{0:F0} km/h | fuel {1:F1}/{2:F0} L | integrity {3:F0}% | reserve {4:F1} L | parts {5} | profile {6}",
                    buggy.Body.velocity.magnitude * 3.6f, state.Fuel, config.TankLitres, state.Integrity, state.ReserveFuel, state.Parts, config.Profile));
        }

        private void Cleanup()
        {
            if (buggy != null) Destroy(buggy.gameObject); buggy = null;
            foreach (GameObject pickup in pickups.Values) if (pickup != null) Destroy(pickup);
            pickups.Clear();
            foreach (Material material in materials) if (material != null) Destroy(material);
            materials.Clear();
        }
        private void OnDestroy() { driver.Release(); Cleanup(); }
        private void OnApplicationQuit() { driver.Release(); }
    }
}
