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
    // Biến để theo dõi thời gian giữa các lần gửi heartbeat đến server
    private float heartbeatTimer;

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
    }

    // Phương thức Update được gọi mỗi khung hình (frame) để kiểm tra và cập nhật các trạng thái trong game
    private void Update()
    {
        // Gọi phương thức HandleLobbyHeartbeat để kiểm tra và gửi heartbeat nếu cần
        HandleLobbyHeartbeat();
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

            CreateLobbyOptions createLobbyOptionsoptions = new CreateLobbyOptions
            {
                IsPrivate = false,
            };
            // Tạo lobby mới với tên và số lượng người chơi tối đa
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayer, createLobbyOptionsoptions);

            // Lưu thông tin của lobby vừa tạo vào biến hostLobby
            hostLobby = lobby;

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
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
            }
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }
    [Command]
    private async void JoinLobby(string lobbyCode)
    {
        try
        {
            // Truy vấn các lobbies từ server với các tùy chọn đã định nghĩa
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }
    [Command]
    private async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode);

            Debug.Log("Join Lobby with code : " + lobbyCode);
        }
        catch (LobbyServiceException e)
        {
            // In ra thông báo lỗi nếu có lỗi xảy ra khi truy vấn lobbies
            Debug.Log("" + e);
        }
    }
    [Command]
    private async void QuickJoinLobby()
    {
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("" + e);
        }
    }
    private void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in Lobby + " + lobby.Name);
        foreach (Player player in lobby.Players)
        {
            Debug.Log(player.Id);
        }
    }
}
