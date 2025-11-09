using Fusion;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Waiting,
    Playing,
}
public class GameLogic : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform spawnpoint;
    [SerializeField] private Transform spawnpointPivot;

    [Networked] private Player Winner { get; set; }
    [Networked, OnChangedRender(nameof(GameStateChanged))] private GameState State {  get; set; }
    [Networked, Capacity(12)] private NetworkDictionary<PlayerRef, Player> Players => default;

    public override void Spawned()
    {
        
        Winner = null;
        State = GameState.Waiting;
        UIManager.Singleton.SetWaitUI(State,Winner);
    }

    public override void FixedUpdateNetwork()
    {
        if (Players.Count < 1)
            return;

        if (Runner.IsServer && State == GameState.Waiting)
        {
            bool areAllReady = true;
            foreach (KeyValuePair<PlayerRef, Player> player in Players)
        {
            if (!player.Value.IsReady)
            {
                areAllReady = false;
                break;
            }
        }

        if (areAllReady)
        {
            Winner = null;
            State = GameState.Playing;
            PreparePlayers();
        }
    }     
}

    private void GameStateChanged()
    {
        UIManager.Singleton.SetWaitUI(State, Winner);
    }

    private void PreparePlayers()
    {
        float spacingAngle = 360f / Players.Count;
        spawnpointPivot.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        foreach (KeyValuePair<PlayerRef, Player> player in Players)
        {
            GetNextSpawnpoint(spacingAngle, out Vector3 position, out Quaternion rotation);
            player.Value.Teleport(position, rotation);
        }
    }

    

    private void OnTriggerEnter(Collider other)
    {
        if (Runner.IsServer && Winner == null && other.attachedRigidbody != null && other.attachedRigidbody.TryGetComponent(out Player player))
        {
            UnreadyAll();
            Winner = player;
            State = GameState.Waiting;
        }
    }


    private void UnreadyAll()
    {
        foreach (KeyValuePair<PlayerRef, Player> player in Players)
            player.Value.IsReady = false;
    }

    private void GetNextSpawnpoint(float spacingAngle, out Vector3 position, out Quaternion rotation)
    {
        position = spawnpoint.position;
        rotation = spawnpoint.rotation;
        spawnpointPivot.Rotate(0f, spacingAngle, 0f);
    }
    public void PlayerJoined(PlayerRef player)
    {
        if (HasStateAuthority)
        {
            GetNextSpawnpoint(90f, out Vector3 position, out Quaternion rotation);
            NetworkObject playerObject = Runner.Spawn(playerPrefab, position, rotation, player);
            Players.Add(player, playerObject.GetComponent<Player>());
        }
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority)
            return;

        if (Players.TryGet(player, out Player playerBehaviour))
        {
            Players.Remove(player);
            Runner.Despawn(playerBehaviour.Object);
        }
    }
}
