using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Discord;

public class DiscordController : MonoBehaviour {

    private Discord.Discord discord;

    void Start () {
        var clientId = ClientIdSender.clientId;
        if (clientId == 0) {
            SceneManager.LoadScene("InputClientId");
        }

        discord = new Discord.Discord(clientId, (System.UInt64)Discord.CreateFlags.Default);

        var userManager = discord.GetUserManager();
        userManager.OnCurrentUserUpdate += () => {
            var currentUser = userManager.GetCurrentUser();
            Debug.LogFormat("current user id: {0}", currentUser.Id);
        };

        var lobbyManager = discord.GetLobbyManager();
        GetOrCreateLobby(lobbyManager, (lobby) => {
            lobbyManager.ConnectVoice(lobby.Id, (result) => {
                if (result == Discord.Result.Ok) {
                    Debug.LogFormat("voice connected: {0}", lobby.Id);
                }
            });
        });
    }

    void GetOrCreateLobby (Discord.LobbyManager lobbyManager, Action<Discord.Lobby> callback) {
        var search = lobbyManager.GetSearchQuery();
        search.Limit(1);
        lobbyManager.Search(search, (result) => {
            if (result == Discord.Result.Ok) {
                if (lobbyManager.LobbyCount() == 1) {
                    var lobbyId = lobbyManager.GetLobbyId(0);
                    var lobby = lobbyManager.GetLobby(lobbyId);
                    callback(lobby);
                } else {
                    var txn = lobbyManager.GetLobbyCreateTransaction();
                    txn.SetCapacity(100);
                    txn.SetType(Discord.LobbyType.Public);
                    lobbyManager.CreateLobby(txn, (Discord.Result result1, ref Discord.Lobby lobby) => {
                        if (result1 == Discord.Result.Ok) {
                            Debug.LogFormat("new lobby created with password `{0}`", lobby.Secret);
                            callback(lobby);
                        }
                    });
                }
            }
        });
    }

    void Update () {
        discord.RunCallbacks();
    }

}
