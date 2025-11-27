using UnityEngine;
using Mirror;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using System.IO;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-room-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkRoomManager.html

	See Also: NetworkManager
	Documentation: https://mirror-networking.gitbook.io/docs/components/network-manager
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkManager.html
*/

/// <summary>
/// This is a specialized NetworkManager that includes a networked room.
/// The room has slots that track the joined players, and a maximum player count that is enforced.
/// It requires that the NetworkRoomPlayer component be on the room player objects.
/// NetworkRoomManager is derived from NetworkManager, and so it implements many of the virtual functions provided by the NetworkManager class.
/// </summary>
public class L2D_NetworkManager : NetworkRoomManager
{
    // Overrides the base singleton so we don't
    // have to cast to this type everywhere.

    [Scene, SerializeField] private string firstSceneToLoad;

    private List<string> sceneNames = new List<string>();
    private bool isSubsceneLoaded = false;
    private bool isInTransition = false;
    public new static L2D_NetworkManager singleton => (L2D_NetworkManager)NetworkRoomManager.singleton;

    public Action<HashSet<NetworkRoomPlayer>> OnAddPlayer;

    #region Server Callbacks

    /// <summary>
    /// This is called on the server when the server is started - including when a host is started.
    /// </summary>
    public override void OnRoomStartServer()
    {
        for (int i = 3; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            sceneNames.Add(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);

        if (sceneName == GameplayScene)
        {
            StartCoroutine(ServerLoadSubscenes());
        }
    }

    private IEnumerator ServerLoadSubscenes()
    {
        foreach (string sceneName in sceneNames)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, new LoadSceneParameters
            {
                loadSceneMode = LoadSceneMode.Additive,
                localPhysicsMode = LocalPhysicsMode.Physics3D
            });
        }

        isSubsceneLoaded = true;
    }

    public override void OnClientSceneChanged()
    {
        if (!isInTransition)
            base.OnClientSceneChanged();
    }

    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        if (sceneOperation == SceneOperation.LoadAdditive)
        {
            StartCoroutine(LoadAdditiveScene(newSceneName));
        }

        if (sceneOperation == SceneOperation.UnloadAdditive)
        {
            StartCoroutine(UnloadAdditiveScene(newSceneName));
        }
    }

    private IEnumerator LoadAdditiveScene(string sceneName)
    {
        isInTransition = true;

        if (mode == NetworkManagerMode.ClientOnly)
        {
            loadingSceneAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            while (loadingSceneAsync != null && !loadingSceneAsync.isDone)
            {
                yield return null;
            }
        }

        NetworkClient.isLoadingScene = false;
        isInTransition = false;

        OnRoomClientSceneChanged();
    }

    private IEnumerator UnloadAdditiveScene(string sceneName)
    {
        isInTransition = true;

        if (mode == NetworkManagerMode.ClientOnly)
        {
            yield return SceneManager.UnloadSceneAsync(sceneName);
            yield return Resources.UnloadUnusedAssets();
        }

        NetworkClient.isLoadingScene = false;
        isInTransition = false;

        OnRoomClientSceneChanged();
    }

    /// <summary>
    /// This is called on the server when a new client connects to the server.
    /// </summary>
    /// <param name="conn">The new connection.</param>
    public override void OnRoomServerConnect(NetworkConnectionToClient conn)
    { }

    /// <summary>
    /// This is called on the server when a client disconnects.
    /// </summary>
    /// <param name="conn">The connection that disconnected.</param>
    public override void OnRoomServerDisconnect(NetworkConnectionToClient conn)
    { }

    /// <summary>
    /// This is called on the server when a networked scene finishes loading.
    /// </summary>
    /// <param name="sceneName">Name of the new scene.</param>
    public override void OnRoomServerSceneChanged(string sceneName)
    { }

    /// <summary>
    /// This allows customization of the creation of the room-player object on the server.
    /// <para>By default the roomPlayerPrefab is used to create the room-player, but this function allows that behaviour to be customized.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    /// <returns>The new room-player object.</returns>
    public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
    {
        OnAddPlayer?.Invoke(roomSlots);

        return base.OnRoomServerCreateRoomPlayer(conn);
    }

    /// <summary>
    /// This allows customization of the creation of the GamePlayer object on the server.
    /// <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    /// <param name="roomPlayer">The room player object for this connection.</param>
    /// <returns>A new GamePlayer object.</returns>
    public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
    {
        L2D_RoomPlayer demoRoomPlayer = roomPlayer.GetComponent<L2D_RoomPlayer>();
        demoRoomPlayer.RpcDisableCanvas();

        return base.OnRoomServerCreateGamePlayer(conn, roomPlayer);
    }

    /// <summary>
    /// This allows customization of the creation of the GamePlayer object on the server.
    /// <para>This is only called for subsequent GamePlay scenes after the first one.</para>
    /// <para>See OnRoomServerCreateGamePlayer to customize the player object for the initial GamePlay scene.</para>
    /// </summary>
    /// <param name="conn">The connection the player object is for.</param>
    public override void OnRoomServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnRoomServerAddPlayer(conn);
    }

    /// <summary>
    /// This is called on the server when it is told that a client has finished switching from the room scene to a game player scene.
    /// <para>When switching from the room, the room-player is replaced with a game-player object. This callback function gives an opportunity to apply state from the room-player to the game-player object.</para>
    /// </summary>
    /// <param name="conn">The connection of the player</param>
    /// <param name="roomPlayer">The room player object.</param>
    /// <param name="gamePlayer">The game player object.</param>
    /// <returns>False to not allow this player to replace the room player.</returns>
    public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
    {
        NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, ReplacePlayerOptions.KeepAuthority);
        StartCoroutine(AddPlayerToGameScene(conn));

        return false;
    }

    private IEnumerator AddPlayerToGameScene(NetworkConnectionToClient conn)
    {
        while (!isSubsceneLoaded)
            yield return null;

        NetworkIdentity[] allObjectWithNetworkIdentity = FindObjectsOfType<NetworkIdentity>();

        foreach (NetworkIdentity identity in allObjectWithNetworkIdentity)
        {
            identity.enabled = true;
        }

        GameObject playerObject = conn.identity.gameObject;

        Debug.Log(playerObject.name);
        NetworkServer.RemovePlayerForConnection(conn, RemovePlayerOptions.KeepActive);

        conn.Send(new SceneMessage { sceneName = firstSceneToLoad, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });
        SceneManager.MoveGameObjectToScene(playerObject, SceneManager.GetSceneByPath(firstSceneToLoad));

        yield return new WaitForEndOfFrame();

        NetworkServer.AddPlayerForConnection(conn, playerObject);
        playerObject.GetComponent<Rigidbody>().isKinematic = false;
    }

    /// <summary>
    /// This is called on server from NetworkRoomPlayer.CmdChangeReadyState when client indicates change in Ready status.
    /// </summary>
    public override void ReadyStatusChanged()
    {
        base.ReadyStatusChanged();
    }

    /// <summary>
    /// This is called on the server when all the players in the room are ready.
    /// <para>The default implementation of this function uses ServerChangeScene() to switch to the game player scene. By implementing this callback you can customize what happens when all the players in the room are ready, such as adding a countdown or a confirmation for a group leader.</para>
    /// </summary>
    public override void OnRoomServerPlayersReady()
    {
        NetworkRoomPlayer host = roomSlots.SingleOrDefault(roomPlayer => roomPlayer.isLocalPlayer && roomPlayer.isServer);
        host?.GetComponent<L2D_RoomPlayer>().SetStartGameInteractable(true);
        //base.OnRoomServerPlayersReady();
    }

    /// <summary>
    /// This is called on the server when CheckReadyToBegin finds that players are not ready
    /// <para>May be called multiple times while not ready players are joining</para>
    /// </summary>
    public override void OnRoomServerPlayersNotReady()
    {
        NetworkRoomPlayer host = roomSlots.SingleOrDefault(roomPlayer => roomPlayer.isLocalPlayer && roomPlayer.isServer);
        host?.GetComponent<L2D_RoomPlayer>().SetStartGameInteractable(false);
    }

    #endregion Server Callbacks

    #region Client Callbacks

    /// <summary>
    /// This is a hook to allow custom behaviour when the game client enters the room.
    /// </summary>
    public override void OnRoomClientEnter()
    { }

    /// <summary>
    /// This is a hook to allow custom behaviour when the game client exits the room.
    /// </summary>
    public override void OnRoomClientExit()
    { }

    /// <summary>
    /// This is called on the client when it connects to server.
    /// </summary>
    public override void OnRoomClientConnect()
    { }

    /// <summary>
    /// This is called on the client when disconnected from a server.
    /// </summary>
    public override void OnRoomClientDisconnect()
    { }

    /// <summary>
    /// This is called on the client when a client is started.
    /// </summary>
    public override void OnRoomStartClient()
    { }

    /// <summary>
    /// This is called on the client when the client stops.
    /// </summary>
    public override void OnRoomStopClient()
    { }

    /// <summary>
    /// This is called on the client when the client is finished loading a new networked scene.
    /// </summary>
    public override void OnRoomClientSceneChanged()
    {
    }

    #endregion Client Callbacks

    #region Optional UI

    public override void OnGUI()
    {
        base.OnGUI();
    }

    #endregion Optional UI
}