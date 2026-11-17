using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("computer_statuses", Schema = "dbo")]
public sealed class ComputerStatus
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column("computer_status_id")]
    public int Id { get; set; }

    [Column("status_name"), MaxLength(30)]
    public required string Name { get; set; }
}
