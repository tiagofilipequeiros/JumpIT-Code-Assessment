namespace Products.Domain.Entities;

public enum Role
{
    User,
    Editor,
    Admin,
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
