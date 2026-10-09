using UnityEngine;

namespace CannibalRally
{
    public static class BuggyFactory
    {
        public static GameObject Create(Vector3 position, Quaternion rotation, RallyConfig config, RallyState state)
        {
            GameObject root = new GameObject("CannibalRally.Buggy");
            root.transform.position = position; root.transform.rotation = rotation;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 650; body.drag = 0.08f; body.angularDrag = 0.6f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0, -0.25f, 0);
            BoxCollider chassis = root.AddComponent<BoxCollider>();
            chassis.center = new Vector3(0, 0.05f, 0); chassis.size = new Vector3(1.5f, 0.6f, 2.8f);
            Material orange = Material(new Color(1, 0.3f, 0.02f));
            Material black = Material(new Color(0.04f, 0.04f, 0.04f));
            Material metal = Material(new Color(0.35f, 0.35f, 0.35f));
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.2f, 0.35f), new Vector3(1.5f, 0.35f, 2.1f), orange);
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.45f, 0.95f), new Vector3(1.45f, 0.2f, 0.7f), orange);
            for (int side = -1; side <= 1; side += 2)
            {
                Part(root, PrimitiveType.Cube, new Vector3(side * 0.65f, 0.85f, -0.3f), new Vector3(0.09f, 1.2f, 0.09f), black);
                Part(root, PrimitiveType.Cube, new Vector3(side * 0.65f, 0.85f, 0.65f), new Vector3(0.09f, 1.2f, 0.09f), black);
                Part(root, PrimitiveType.Cube, new Vector3(side * 0.65f, 1.45f, 0.18f), new Vector3(0.09f, 0.09f, 1.15f), black);
                Part(root, PrimitiveType.Cube, new Vector3(side * 0.38f, 0.5f, -0.3f), new Vector3(0.5f, 0.35f, 0.6f), black);
            }
            Part(root, PrimitiveType.Cube, new Vector3(0, 1.45f, -0.3f), new Vector3(1.35f, 0.09f, 0.09f), black);
            Part(root, PrimitiveType.Cube, new Vector3(0, 0, 1.5f), new Vector3(1.6f, 0.15f, 0.15f), black);
            WheelCollider[] wheels = new WheelCollider[4]; Transform[] meshes = new Transform[4];
            for (int i = 0; i < 4; ++i)
            {
                Vector3 local = new Vector3(i % 2 == 0 ? -0.9f : 0.9f, 0, i < 2 ? 1 : -1);
                GameObject wheel = new GameObject("Suspension" + i);
                wheel.transform.parent = root.transform; wheel.transform.localPosition = local;
                wheel.transform.localRotation = Quaternion.identity;
                wheels[i] = wheel.AddComponent<WheelCollider>();
                wheels[i].radius = 0.4f; wheels[i].mass = 25; wheels[i].suspensionDistance = 0.3f;
                JointSpring spring = wheels[i].suspensionSpring;
                spring.spring = 28000; spring.damper = 4000; spring.targetPosition = 0.5f;
                wheels[i].suspensionSpring = spring;
                GameObject visual = new GameObject("WheelVisual" + i);
                visual.transform.parent = root.transform;
                Transform tyre = Part(visual, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.8f, 0.15f, 0.8f), black);
                tyre.localRotation = Quaternion.Euler(0, 0, 90);
                Transform hub = Part(visual, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.42f, 0.16f, 0.42f), metal);
                hub.localRotation = Quaternion.Euler(0, 0, 90);
                meshes[i] = visual.transform;
            }
            GameObject lamp = new GameObject("Headlights"); lamp.transform.parent = root.transform;
            lamp.transform.localPosition = new Vector3(0, 0.6f, 1.4f); lamp.transform.localRotation = Quaternion.identity;
            Light light = lamp.AddComponent<Light>(); light.type = LightType.Spot;
            light.range = 45; light.spotAngle = 70; light.intensity = 1.5f;
            BuggyVehicle vehicle = root.AddComponent<BuggyVehicle>();
            vehicle.Configure(body, wheels, meshes, config, state);
            return root;
        }

        public static Material Material(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            if (shader == null) throw new System.InvalidOperationException("No supported shader.");
            Material result = new Material(shader); result.color = color; return result;
        }

        public static Transform Part(GameObject root, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            Collider collider = part.GetComponent<Collider>(); collider.enabled = false; Object.Destroy(collider);
            part.transform.parent = root.transform; part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.identity; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }
    }
}
