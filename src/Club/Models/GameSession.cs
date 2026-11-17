using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("sessions", Schema = "dbo")]
public sealed class GameSession
{
    [Key, Column("session_id")]
    public int Id { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("computer_id")]
    public int ComputerId { get; set; }

    [Column("tariff_id")]
    public int TariffId { get; set; }

    [Column("start_time", TypeName = "datetime2(0)")]
    public DateTime StartTime { get; set; }

    [Column("end_time", TypeName = "datetime2(0)")]
    public DateTime? EndTime { get; set; }

    [Column("session_status_id")]
    public int StatusId { get; set; }
}
