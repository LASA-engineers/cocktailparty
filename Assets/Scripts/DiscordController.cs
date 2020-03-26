using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Discord;

public class DiscordController : MonoBehaviour {

    public GameObject canvasPrefab;

    private Discord.Discord discord;
    private Dictionary<long, GameObject> userIdToCanvas;
    private List<long> userIdOrder;

    void Start () {
        var clientId = ClientIdSender.clientId;
        if (clientId == 0) {
            SceneManager.LoadScene("InputClientId");
        }

        userIdToCanvas = new Dictionary<long, GameObject>();
        userIdOrder = new List<long>();

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
                lobbyManager.OnMemberDisconnect += (lobbyId, userId) => {
                    DeleteUser(lobbyManager, lobby.Id, userId);
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
        GameObject canvas = Instantiate(canvasPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        canvas.transform.SetParent(GameObject.Find("Canvas").transform, false);
        SetCanvasPosition(canvas, userIdToCanvas.Count);
        userIdToCanvas.Add(userId, canvas);
        userIdOrder.Add(userId);

        var image = canvas.GetComponentInChildren<Image>();
        var imageManager = discord.GetImageManager();
        imageManager.Fetch(Discord.ImageHandle.User(userId, 128), (result, handle) => {
            if (result == Discord.Result.Ok) {
                var size = imageManager.GetDimensions(handle);
                var rgba = imageManager.GetData(handle);
                // flip upside down
                var flipped = new byte[rgba.Length];
                for (int y = 0; y < size.Height; y++) {
                    Array.Copy(rgba, size.Width * y * 4, flipped, size.Width * (size.Height - y - 1) * 4, size.Width * 4);
                }
                var texture = new Texture2D((int)size.Width, (int)size.Height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(flipped);
                texture.Apply();
                var rect = new Rect(0, 0, size.Width, size.Width);
                image.overrideSprite = Sprite.Create(texture, rect, Vector2.zero);
            }
        });

        var text = canvas.GetComponentInChildren<Text>();
        text.text = user.Username;
    }

    void DeleteUser(Discord.LobbyManager lobbyManager, long lobbyId, long userId) {
        Destroy(userIdToCanvas[userId]);
        userIdToCanvas.Remove(userId);
        userIdOrder.Remove(userId);
        for (int i = 0; i < userIdOrder.Count; i++) {
            SetCanvasPosition(userIdToCanvas[userIdOrder[i]], i);
        }
    }

    void SetCanvasPosition(GameObject canvas, int order) {
        foreach (Transform child in canvas.transform) {
            child.Translate(0, 240 - order * 120, 0);
        }
    }

    void Update () {
        discord.RunCallbacks();
    }

}
