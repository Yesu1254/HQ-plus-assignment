using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiCore.SignalR
{
    public class EventHub: Hub
    {
        public EventHub()
        {

        }


        public override async Task OnConnectedAsync()
        {
            //var httpContext = Context.Connection.GetHttpContext();
            //var sessionId = Context.Connection.GetHttpContext().Request.Query["sessionId"];
            //var user = Context.UserIdentifier;
            var qs = this.Context.GetHttpContext().Request.QueryString;

            var token = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(qs.Value)["t"];

           // var mxUser = await StateManager.GetUserByToken(token);// as IMobileWorxUser;

           // if (mxUser == null) mxUser = StateManager.LoginUser(token, true);// as IMobileWorxUser;

           // if (mxUser == null) return;

           // if (mxUser.signalRConnectionIds == null) mxUser.signalRConnectionIds = new System.Collections.Generic.List<string>();
           // mxUser.signalRConnectionIds.Add(Context.ConnectionId);

            await base.OnConnectedAsync();

            return;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            //var users = StateManager.ActiveUsers.Values.ToList().Where(u => u.signalRConnectionIds.Contains(Context.UserIdentifier)).ToList();

            //foreach (var user in users)
            //{
            //    var u = user;// as IMobileWorxUser;
            //    u.signalRConnectionIds = u.signalRConnectionIds.Where(x => x != Context.UserIdentifier).ToList();
            //}

            return base.OnDisconnectedAsync(exception);
        }

        public Task Send(string message)
        {
            return Clients.All.SendAsync("Send", message);
        }

        //public override Task OnConnected()
        //{
        //    try
        //    {
        //        string token = Context.QueryString["token"];

        //        if (String.IsNullOrWhiteSpace(token)) return base.OnConnected();

        //        var user = StateManager.GetUserByToken(token).Result;

        //        if (user == null) return base.OnConnected(); //invalid token

        //        user.signalRConnectionIds.Add(Context.ConnectionId);
        //    }
        //    catch (Exception ex)
        //    {
        //        //SystemEventManager.AddEvent($"SignalR OnConnected exception: {ex.Message}",4);
        //        System.IO.File.AppendAllText(GlobalsManager.GetCurrentDirectory + "Errors.txt", $"SignalR OnConnected exception: {ex.Message}\r\n");
        //    }

        //    return base.OnConnected();
        //}

        //public override Task OnReconnected()
        //{
        //    try
        //    {
        //        string token = Context.QueryString["token"];

        //        if (String.IsNullOrWhiteSpace(token)) return base.OnConnected();

        //        var user = StateManager.GetUserByToken(token).Result;

        //        user.signalRConnectionIds.Add(Context.ConnectionId);

        //    }
        //    catch (Exception ex)
        //    {
        //        //SystemEventManager.AddEvent($"SignalR OnConnected exception: {ex.Message}",4);
        //        System.IO.File.AppendAllText(GlobalsManager.GetCurrentDirectory + "Errors.txt", $"SignalR OnReconnected exception: {ex.Message}\r\n");
        //    }

        //    return base.OnReconnected();
        //}

        //public override Task OnDisconnected(bool stopCalled)
        //{
        //    try
        //    {
        //        var user = StateManager.GetUserBySignalRConnectionId(Context.ConnectionId);
        //        if (user == null) return base.OnDisconnected(stopCalled);

        //        try
        //        {
        //            user.signalRConnectionIds.Remove(Context.ConnectionId);
        //        }
        //        catch (Exception ex)
        //        {
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        //SystemEventManager.AddEvent($"SignalR OnConnected exception: {ex.Message}",4);
        //        System.IO.File.AppendAllText(GlobalsManager.GetCurrentDirectory + "Errors.txt", $"SignalR OnDisconnected exception: {ex.Message}\r\n");
        //    }
        //    return base.OnDisconnected(stopCalled);
        //}

        //public object Command(string command, string args)
        //{
        //    string token = Context.QueryString["token"];

        //    if (String.IsNullOrWhiteSpace(token)) return base.OnConnected();

        //    var user = StateManager.GetUserByToken(token).Result;

        //    StateManager.SetClaimsIdentity(user.id);

        //    //var routeResult = ActionDelegator.RouteRequest(response, pathAndQuery, request.Method, requestContentStr, primaryAccept, currentUser, request);

        //    var routeRequest = new RouteRequest();
        //    routeRequest.user = user;
        //    routeRequest.service = command;
        //    routeRequest.contentStr = args;

        //    var result = RouteManager.Process("s", command, routeRequest).Result;

        //    var jsonResult = JsonManager.Serialize(result);

        //    return jsonResult;
        //}

        //public void GroupMessage(string group, string message)
        //{
        //    Send(group, message);
        //}


        //public static bool Send(string group, string message)
        //{
        //    var context = GlobalHost.ConnectionManager.GetHubContext<EventHub>();
        //    var subscribedUserConnectionIds = StateManager.ActiveUsers.Values.Where(u => u.signalRSubscriptions.Contains(group)).SelectMany(u => u.signalRConnectionIds).ToList();
        //    var clients = context.Clients.Clients(subscribedUserConnectionIds);
        //    clients.sendToClient(group, message);
        //    return true;
        //}

        //public static bool Send(IUser user, string message)
        //{
        //    var context = GlobalHost.ConnectionManager.GetHubContext<EventHub>();
        //    if (user == null || user.signalRConnectionIds.Count == 0) return false;
        //    var clients = context.Clients.Clients(user.signalRConnectionIds);
        //    clients.sendMessage(message);
        //    return true;
        //}
    }
}
