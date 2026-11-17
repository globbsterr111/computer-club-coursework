using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("session_statuses", Schema = "dbo")]
public sealed class SessionStatus
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column("session_status_id")]
    public int Id { get; set; }

    [Column("status_name"), MaxLength(30)]
    public required string Name { get; set; }
}
