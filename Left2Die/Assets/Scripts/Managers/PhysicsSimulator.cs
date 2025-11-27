using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PhysicsSimulator : MonoBehaviour
{
    private PhysicsScene2D physics2D;
    private PhysicsScene physics;

    private bool simulatePhysics2D;
    private bool simulatePhysics;

    // Start is called before the first frame update
    private void Awake()
    {
        if (NetworkServer.active)
        {
            physics2D = gameObject.scene.GetPhysicsScene2D();
            physics = gameObject.scene.GetPhysicsScene();

            simulatePhysics2D = physics2D.IsValid() && physics2D != Physics2D.defaultPhysicsScene;
            simulatePhysics = physics.IsValid() && physics != Physics.defaultPhysicsScene;
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!NetworkServer.active)
            return;

        if (simulatePhysics2D)
        {
            physics2D.Simulate(Time.fixedDeltaTime);
        }
        if (simulatePhysics)
        {
            physics.Simulate(Time.fixedDeltaTime);
        }
    }
}