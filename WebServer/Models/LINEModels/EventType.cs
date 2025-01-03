namespace WebServer.Models.LINEModels;

public enum EventType
{
    message,
    unsend,
    follow,
    unfollow,
    join,
    leave,
    memberJoined,
    memberLeft,
    postback,
    videoPlayComplete,
    beacon,
    accountLink,
    things,
}