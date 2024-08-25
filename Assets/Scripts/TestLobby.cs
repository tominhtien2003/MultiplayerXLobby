using System.Collections.Generic;
using QFSW.QC;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

// Lớp TestLobby thừa kế từ MonoBehaviour, đại diện cho một component trong Unity
public class TestLobby : MonoBehaviour
{
    // Biến để lưu trữ thông tin của lobby mà người chơi tạo ra
    private Lobby hostLobby;
    private Lobby joinedLobby;
    // Biến để theo dõi thời gian giữa các lần gửi heartbeat đến server
    //Heartbeat : Tín hiệu
    private float heartbeatTimer;
    // Biến để theo dõi thời gian giữa các lần cập nhật thông tin từ server
    private float lobbyUpdateTimer;

    // Biến để lưu trữ tên người chơi
    private string playerName;

    // Phương thức Start là phương thức đầu tiên được gọi khi đối tượng này được khởi tạo trong Unity
    private async void Start()
    {
        // Khởi tạo Unity Services (các dịch vụ của Unity như Authentication, Lobby, v.v.)
        await UnityServices.InitializeAsync();

        // Đăng ký một hành động khi người dùng đã đăng nhập thành công
        AuthenticationService.Instance.SignedIn += () =>
        {
            // In ra thông báo khi người chơi đăng nhập thành công, kèm theo PlayerId
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        // Đăng nhập ẩn danh người dùng (không cần tài khoản)
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Tạo một tên người chơi ngẫu nhiên với phần đuôi là một số ngẫu nhiên từ 0 đến 99
        playerName = "CodeMinhTien " + Random.Range(0, 100);

        // In ra tên người chơi để kiểm tra
        Debug.Log(playerName);
    }

    // Phương thức Update được gọi mỗi khung hình (frame) để kiểm tra và cập nhật các trạng thái trong game
    private void Update()
    {
        // Gọi phương thức HandleLobbyHeartbeat để kiểm tra và gửi heartbeat nếu cần
        HandleLobbyHeartbeat();
        // Gọi phương thức HandleLobbyPollForUpdate để kiểm tra và cập nhật thông tin lobby nếu cần
        HandleLobbyPollForUpdate();
    }

    // Phương thức để gửi heartbeat đến server, giữ cho lobby không bị đóng khi không có hoạt động
    private async void HandleLobbyHeartbeat()
    {
        // Kiểm tra nếu có lobby được tạo
        if (hostLobby != null)
        {
            // Giảm thời gian heartbeat timer theo thời gian đã trôi qua kể từ khung hình cuối
            heartbeatTimer -= Time.deltaTime;

            // Nếu timer đã hết (dưới 0)
            if (heartbeatTimer < 0)
            {
                // Đặt lại timer về giá trị tối đa (15 giây)
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                // Gửi tín hiệu heartbeat đến server để duy trì lobby
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }
    // Phương thức để cập nhật thông tin lobby từ server
    private async void HandleLobbyPollForUpdate()
    {
        // Kiểm tra nếu đã tham gia vào một lobby
        if (joinedLobby != null)
        {
            // Giảm thời gian update timer theo thời gian đã trôi qua kể từ khung hình cuối
            lobbyUpdateTimer -= Time.deltaTime;

            // Nếu timer đã hết (dưới 0)
            if (lobbyUpdateTimer < 0)
            {
                // Đặt lại timer về giá trị tối đa (1.1 giây)
                float lobbyUpdateTimerMax = 1.1f;
                heartbeatTimer = lobbyUpdateTimerMax;

                // Lấy thông tin mới nhất của lobby từ server
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                // Cập nhật thông tin lobby mà người chơi đã tham gia
                joinedLobby = lobby;
            }
        }
    }

    // Phương thức để tạo lobby mới, có thể được gọi từ console nhờ vào thuộc tính [Command]
    [Command]
    private async void CreateLobby()
    {
        try
        {
            // Tên của lobby sẽ được tạo
            string lobbyName = "MyLobby";
            // Số lượng người chơi tối đa có thể tham gia lobby này
            int maxPlayer = 4;

            // Tùy chọn để tạo lobby, đặt nó thành công khai và gán người chơi vào lobby
            CreateLobbyOptions createLobbyOptionsoptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>{
                    {"GameMode",new DataObject(DataObject.VisibilityOptions.Public,"CaptureTheFlag")},
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public,"de_dust2")}
                }
            };
            // Tạo lobby mới với tên và số lượng người chơi tối đa
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptionsoptions);

            // Lưu thông tin của lobby vừa tạo vào biến hostLobby
            hostLobby = lobby;
            joinedLobby = hostLobby;

            // In ra danh sách người chơi trong lobby
            PrintPlayers(hostLobby);

            // In ra thông tin của lobby mới tạo, bao gồm tên và số lượng người chơi tối đa
            Debug.Log("Create Lobby : " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi tạo lobby
            Debug.Log("" + e);
        }
    }

    // Phương thức để liệt kê các lobbies hiện có, có thể được gọi từ console nhờ vào thuộc tính [Command]
    [Command]
    private async void ListLobbies()
    {
        try
        {
            // Tạo các tùy chọn truy vấn lobbies
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions()
            {
                // Giới hạn số lượng lobbies trả về là 25
                Count = 25,

                // Lọc các lobbies có ít nhất 1 chỗ trống (có số lượng slot trống lớn hơn 0)
                Filters = new List<QueryFilter>(){
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots,"0",QueryFilter.OpOptions.GT)
                },

                // Sắp xếp các lobbies theo thời gian tạo, từ mới nhất đến cũ nhất
                Order = new List<QueryOrder>(){
                    new QueryOrder(false,QueryOrder.FieldOptions.Created)
                }
            };

            // Truy vấn các lobbies từ server với các tùy chọn đã định nghĩa
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            // In ra số lượng lobbies tìm thấy
            Debug.Log("Lobbies found : " + queryResponse.Results.Count);

            // Duyệt qua từng lobby trong danh sách kết quả và in ra tên và số lượng người chơi tối đa của chúng
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }

    // Phương thức để tham gia vào một lobby theo mã lobby, có thể được gọi từ console
    [Command]
    private async void JoinLobby(string lobbyCode)
    {
        try
        {
            // Truy vấn các lobbies từ server với các tùy chọn đã định nghĩa
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            // Tham gia vào lobby đầu tiên tìm thấy từ kết quả truy vấn
            await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }

    // Phương thức để tham gia vào một lobby theo mã code, có thể được gọi từ console
    [Command]
    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            // Tùy chọn để tham gia lobby với mã code, gán người chơi vào lobby
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };

            // Tham gia vào lobby theo mã code
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            joinedLobby = lobby;
            // In ra thông báo đã tham gia thành công và in ra danh sách người chơi
            Debug.Log("Join Lobby with code : " + lobbyCode);

            PrintPlayers(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }

    // Phương thức để nhanh chóng tham gia vào một lobby bất kỳ, có thể được gọi từ console
    [Command]
    private async void QuickJoinLobby()
    {
        try
        {
            // Tham gia nhanh vào một lobby bất kỳ
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra
            Debug.Log("" + e);
        }
    }

    // Phương thức để tạo và trả về đối tượng người chơi (Player)
    private Player GetPlayer()
    {
        return new Player
        {
            // Gán dữ liệu người chơi bao gồm tên người chơi vào từ điển Data
            Data = new Dictionary<string, PlayerDataObject>{
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,playerName)}
            }
        };
    }
    [Command]
    private void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }
    // Phương thức để in ra danh sách người chơi trong một lobby cụ thể
    private void PrintPlayers(Lobby lobby)
    {
        // In ra tên của lobby và danh sách người chơi
        Debug.Log("Players in Lobby + " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
        foreach (Player player in lobby.Players)
        {
            // In ra ID của người chơi và tên của họ
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }
    [Command]
    private async void UpdateLobbyGameMode(string gameMode)
    {
        try
        {
            // Gửi yêu cầu cập nhật thông tin của lobby, thay đổi chế độ trò chơi (game mode) của lobby
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                // Tạo một dictionary để lưu trữ dữ liệu cần cập nhật
                Data = new Dictionary<string, DataObject>{
                // Cập nhật dữ liệu "GameMode" với giá trị mới và độ công khai là công khai (Public)
                {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode)}
            }
            });
            // Cập nhật thông tin lobby hiện tại với dữ liệu mới
            joinedLobby = hostLobby;

            // Gọi phương thức PrintPlayers để in ra danh sách người chơi trong lobby
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi cập nhật lobby
            Debug.Log("" + e);
        }
    }

    [Command]
    private async void UpdatePlayerName(string newPlayerName)
    {
        try
        {
            // Cập nhật tên người chơi với giá trị mới
            playerName = newPlayerName;

            // Gửi yêu cầu cập nhật thông tin người chơi trong lobby
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
            {
                // Tạo một dictionary để lưu trữ dữ liệu cần cập nhật
                Data = new Dictionary<string, PlayerDataObject>{
                // Cập nhật dữ liệu "PlayerName" với giá trị mới và độ công khai là thành viên (Member)
                {"PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)}
            }
            });
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi cập nhật tên người chơi
            Debug.Log("" + e);
        }
    }

    [Command]
    private async void LeaveLobby()
    {
        try
        {
            // Gửi yêu cầu rời khỏi lobby cho người chơi hiện tại
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi rời khỏi lobby
            Debug.Log("" + e);
        }
    }

    [Command]
    private async void KickPlayer()
    {
        try
        {
            // Gửi yêu cầu loại bỏ một người chơi khỏi lobby (ở đây loại bỏ người chơi thứ hai trong danh sách)
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, joinedLobby.Players[1].Id);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi loại bỏ người chơi
            Debug.Log("" + e);
        }
    }
    [Command]
    private async void MigrateLobbyHost()
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                HostId = joinedLobby.Players[1].Id
            });
            joinedLobby = hostLobby;

            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("" + e);
        }
    }
    [Command]
    private void DeletedLobby()
    {
        try
        {
            LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("" + e);
        }
    }
}
