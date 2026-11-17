using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("computers", Schema = "dbo")]
public sealed class Computer
{
    [Key, Column("computer_id")]
    public int Id { get; set; }

    [Column("computer_name"), MaxLength(30)]
    public required string Name { get; set; }

    [Column("zone_name"), MaxLength(40)]
    public required string Zone { get; set; }

    [Column("computer_status_id")]
    public int StatusId { get; set; }
}
