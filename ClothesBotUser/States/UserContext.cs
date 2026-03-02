namespace ClothesBotUser.States;

public enum UserStep { None, AwaitingComment }

public class UserContext 
{ 
    public UserStep Step { get; set; } = UserStep.None; 
    public int PendingItemId { get; set; } 
}