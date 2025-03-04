// Models/Permissions.cs
public static class Permissions
{
    public const string ViewDashboard = "ViewDashboard";
    public const string ManageChatbots = "ManageChatbots";
    public const string ViewChatbots = "ViewChatbots";
    public const string CreateChatbots = "CreateChatbots";
    public const string EditChatbots = "EditChatbots";
    public const string DeleteChatbots = "DeleteChatbots";
    public const string ViewKnowledgeBases = "ViewKnowledgeBases";
    public const string ManageKnowledgeBases = "ManageKnowledgeBases";
    public const string ViewUsers = "ViewUsers";
    public const string ManageUsers = "ManageUsers";
    public const string ManageSettings = "ManageSettings";
    
    public static List<string> GetAllPermissions()
    {
        return typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null))
            .ToList();
    }
}