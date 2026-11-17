using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("users", Schema = "dbo")]
public sealed class UserAccount
{
    [Key, Column("user_id")]
    public int Id { get; set; }

    [Column("login"), MaxLength(50)]
    public required string Login { get; set; }

    [Column("password_hash"), MaxLength(500)]
    public required string PasswordHash { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("client_id")]
    public int? ClientId { get; set; }
}
