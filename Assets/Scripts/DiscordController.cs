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

            var lobbyManager = discord.GetLobbyManager();
            var voiceManager = discord.GetVoiceManager();
            GetOrCreateLobby(lobbyManager, (lobby) => {
                lobbyManager.ConnectVoice(lobby.Id, (result) => {
                    if (result == Discord.Result.Ok) {
                        Debug.LogFormat("voice connected: {0}", lobby.Id);
                        Debug.LogFormat("local volume for owner {0} is {1}", lobby.OwnerId, voiceManager.GetLocalVolume(lobby.OwnerId));
                        Debug.LogFormat("Mute: {0}, Deaf: {1}", voiceManager.IsSelfMute(), voiceManager.IsSelfDeaf());
                    }
                });
                for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++) {
                    var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
                    if (userId != currentUser.Id) {
                        ShowUser(lobbyManager, lobby.Id, userId);
                    }
                }
                lobbyManager.OnMemberConnect += (lobbyId, userId) => {
                    ShowUser(lobbyManager, lobbyId, userId);
                };
            });
        };
    }

    void GetOrCreateLobby (Discord.LobbyManager lobbyManager, Action<Discord.Lobby> callback) {
        var search = lobbyManager.GetSearchQuery();
        search.Limit(1);
        lobbyManager.Search(search, (result) => {
            if (result == Discord.Result.Ok) {
                if (lobbyManager.LobbyCount() == 1) {
                    var lobbyId = lobbyManager.GetLobbyId(0);
                    Debug.LogFormat("existing lobby found: {0}", lobbyId);
                    lobbyManager.ConnectLobby(lobbyId, lobbyManager.GetLobbyMetadataValue(lobbyId, "Secret"), (Discord.Result result1, ref Discord.Lobby lobby) => {
                        if (result1 == Discord.Result.Ok) {
                            Debug.Log("existing lobby connected");
                            callback(lobby);
                        }
                    });
                } else {
                    var txnCreate = lobbyManager.GetLobbyCreateTransaction();
                    txnCreate.SetCapacity(100);
                    txnCreate.SetType(Discord.LobbyType.Public);
                    lobbyManager.CreateLobby(txnCreate, (Discord.Result result1, ref Discord.Lobby lobby) => {
                        if (result1 == Discord.Result.Ok) {
                            Debug.LogFormat("new lobby created: {0}", lobby.Id);
                            var txnUpdate = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
                            txnUpdate.SetMetadata("Secret", lobby.Secret);
                            lobbyManager.UpdateLobby(lobby.Id, txnUpdate, (Discord.Result result2) => {
                                if (result2 == Discord.Result.Ok) {
                                    Debug.Log("lobby secret registered");
                                }
                            });
                            callback(lobby);
                        }
                    });
                }
            }
        });
    }

    void ShowUser(Discord.LobbyManager lobbyManager, long lobbyId, long userId) {
        var user = lobbyManager.GetMemberUser(lobbyId, userId);
        Debug.LogFormat("Got user {0}", user.Username);
    }

    void Update () {
        discord.RunCallbacks();
    }

}
