using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float damage;

    [SerializeField] private float speed = 100;

    private void Awake()
    {
        Destroy(this.gameObject, 10);
    }

    private void LateUpdate()
    {
        DoMovement();
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(this.gameObject);
    }

    public void SetDamage(float damageInput)
    {
        this.damage = damageInput;
    }

    private void DoMovement()
    {
        this.gameObject.transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}