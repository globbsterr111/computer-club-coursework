using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("clients", Schema = "dbo")]
public sealed class Client
{
    [Key, Column("client_id")]
    public int Id { get; set; }

    [Column("last_name"), MaxLength(60)]
    public required string LastName { get; set; }

    [Column("first_name"), MaxLength(60)]
    public required string FirstName { get; set; }

    [Column("middle_name"), MaxLength(60)]
    public string? MiddleName { get; set; }

    [Column("phone"), MaxLength(20)]
    public required string Phone { get; set; }

    [Column("created_at", TypeName = "datetime2(0)")]
    public DateTime CreatedAt { get; set; }
}
