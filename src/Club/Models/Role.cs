using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("roles", Schema = "dbo")]
public sealed class Role
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column("role_id")]
    public int Id { get; set; }

    [Column("role_name"), MaxLength(30)]
    public required string Name { get; set; }
}
