using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 2f;

    [SerializeField] private Transform bulletSpawnpoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera cameraView;

    private void Awake()
    {
        this.rb = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isLocalPlayer)
            return;

        DoAttack();
    }

    private void LateUpdate()
    {
        if (!isLocalPlayer)
            return;

        DoMovement();
        DoRotation();
    }

    private void OnTriggerEnter(Collider other)
    {
        Bullet bullet = other.GetComponent<Bullet>();

        if (bullet != null)
        {
            RpcSendMessage("Player got hit!");
        }
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        this.cameraView.gameObject.SetActive(true);
    }

    private void DoMovement()
    {
        if (Input.GetKey(KeyCode.W))
        {
            this.gameObject.transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S))
        {
            this.gameObject.transform.Translate(Vector3.back * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A))
        {
            this.gameObject.transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D))
        {
            this.gameObject.transform.Translate(Vector3.right * speed * Time.deltaTime);
        }
    }

    private void DoRotation()
    {
        // No vertical rotation because I am having a hard time programming it and honestly it makes me kinda motion sick
        float horizontal = rotationSpeed * (Input.GetAxis("Mouse X"));

        transform.Rotate(0, horizontal, 0);
    }

    private void DoAttack()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CmdSpawnBullet(bulletSpawnpoint.position, bulletSpawnpoint.rotation);
        }
    }

    [Command]
    private void CmdSpawnBullet(Vector3 position, Quaternion rotation)
    {
        CmdSendMessage("spdsfks");
        GameObject bullet = Instantiate<GameObject>(bulletPrefab, position, rotation);
        NetworkServer.Spawn(bullet, this.gameObject);
    }

    [Command]
    private void CmdSendMessage(string messgeToSend)
    {
        RpcSendMessage(messgeToSend);
    }

    [ClientRpc]
    private void RpcSendMessage(string message)
    {
        Debug.Log(message);
    }
}