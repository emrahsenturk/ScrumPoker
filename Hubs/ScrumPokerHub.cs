using Microsoft.AspNetCore.SignalR;
using ScrumPoker.Models;

namespace ScrumPoker.Hubs;

public class ScrumPokerHub : Hub
{
    private static Dictionary<string, PokerSession> _sessions = new();

    public async Task<CreateRoomResult> CreateRoom(string userName)
    {
        var roomId = Guid.NewGuid().ToString("N");
        var session = new PokerSession 
        { 
            SessionId = roomId,
            Players = new List<Player>
            {
                new Player 
                { 
                    UserName = userName,
                    ConnectionId = Context.ConnectionId 
                }
            }
        };

        _sessions[roomId] = session;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        
        return new CreateRoomResult 
        { 
            Success = true,
            RoomId = roomId
        };
    }

    public async Task<JoinResult> JoinRoom(string roomId, string userName)
    {
        if (!_sessions.ContainsKey(roomId))
        {
            return new JoinResult 
            { 
                Success = false,
                ErrorMessage = "Oda bulunamadı." 
            };
        }

        var session = _sessions[roomId];
        var existingPlayer = session.Players.FirstOrDefault(p => 
            p.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

        if (session.Players.Count == 1 && 
            session.Players[0].UserName.Equals(userName, StringComparison.OrdinalIgnoreCase))
        {
            session.Players[0].ConnectionId = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            return new JoinResult 
            { 
                Success = true,
                Session = session 
            };
        }

        if (existingPlayer != null)
        {
            if (existingPlayer.ConnectionId == Context.ConnectionId)
            {
                return new JoinResult 
                { 
                    Success = true,
                    Session = session 
                };
            }
            return new JoinResult 
            { 
                Success = false,
                ErrorMessage = "Bu kullanıcı adı zaten kullanımda. Lütfen başka bir isim seçin."
            };
        }

        var player = new Player 
        { 
            UserName = userName,
            ConnectionId = Context.ConnectionId 
        };

        session.Players.Add(player);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.OthersInGroup(roomId).SendAsync("UserJoined", userName);
        await Clients.Group(roomId).SendAsync("UpdateSession", session);

        return new JoinResult 
        { 
            Success = true,
            Session = session 
        };
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var session = _sessions.Values.FirstOrDefault(s => 
            s.Players.Any(p => p.ConnectionId == Context.ConnectionId));

        if (session != null)
        {
            var player = session.Players.First(p => p.ConnectionId == Context.ConnectionId);
            session.Players.Remove(player);
            
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.SessionId);
            await Clients.Group(session.SessionId).SendAsync("PlayerLeft", player.UserName);
            await Clients.Group(session.SessionId).SendAsync("UpdateSession", session);

            if (session.Players.Count == 0)
            {
                _sessions.Remove(session.SessionId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Vote(string roomId, string userName, string vote)
    {
        if (!_sessions.ContainsKey(roomId)) return;

        var session = _sessions[roomId];
        var player = session.Players.FirstOrDefault(p => p.UserName == userName);
        
        if (player != null)
        {
            player.Vote = vote;
            await Clients.Group(roomId).SendAsync("VoteReceived", userName, vote);

            if (session.Players.All(p => p.HasVoted))
            {
                session.VotesRevealed = true;
                await Clients.Group(roomId).SendAsync("VotesRevealed");
            }

            await Clients.Group(roomId).SendAsync("UpdateSession", session);
        }
    }

    public async Task RevealVotes(string roomId)
    {
        if (!_sessions.ContainsKey(roomId)) return;

        var session = _sessions[roomId];
        session.VotesRevealed = true;
        await Clients.Group(roomId).SendAsync("VotesRevealed");
        await Clients.Group(roomId).SendAsync("UpdateSession", session);
    }

    public async Task ResetVotes(string roomId)
    {
        if (!_sessions.ContainsKey(roomId)) return;

        var session = _sessions[roomId];
        foreach (var player in session.Players)
        {
            player.Vote = null;
        }
        session.VotesRevealed = false;
        await Clients.Group(roomId).SendAsync("VotesReset");
        await Clients.Group(roomId).SendAsync("UpdateSession", session);
    }

    public async Task<RoomJoinResult> CheckRoom(string roomId)
    {
        return new RoomJoinResult
        {
            Success = true,
            RoomExists = _sessions.ContainsKey(roomId)
        };
    }
} 