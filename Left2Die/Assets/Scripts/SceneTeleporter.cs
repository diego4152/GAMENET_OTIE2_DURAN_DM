using Mirror;
using Mirror.Examples.CouchCoop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleporter : NetworkBehaviour
{
    [Scene, SerializeField] private string sceneToTeleportTo;

    [SerializeField] private string spawnName;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        if (other.GetComponent<Player>())
        {
            StartCoroutine(SendPlayer(other.gameObject));
        }
    }

    [ServerCallback]
    private IEnumerator SendPlayer(GameObject player)
    {
        NetworkIdentity identity = player.GetComponent<NetworkIdentity>();
        if (identity == null)
            yield break;

        NetworkConnectionToClient conn = identity.connectionToClient;
        if (conn == null)
            yield break;

        conn.Send(new SceneMessage { sceneName = this.gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });
        NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.KeepActive);

        conn.Send(new SceneMessage { sceneName = sceneToTeleportTo, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });
        SceneManager.MoveGameObjectToScene(player, SceneManager.GetSceneByPath(sceneToTeleportTo));

        NetworkStartPosition[] positions = GameObject.FindObjectsOfType<NetworkStartPosition>();
        Vector3 position = Vector3.zero;
        foreach (NetworkStartPosition pos in positions)
        {
            if (pos.gameObject.scene.path == sceneToTeleportTo && pos.gameObject.name == spawnName)
            {
                position = pos.transform.position;
                break;
            }
        }

        player.transform.position = position;

        yield return new WaitForEndOfFrame();

        NetworkServer.AddPlayerForConnection(conn, player);
        player.GetComponent<Rigidbody>().isKinematic = false;
    }
}