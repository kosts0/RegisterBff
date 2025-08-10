using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkerService1.DbEntity;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public long Oid { get; set; }
    public DateTimeOffset? LastTimeUpdated { get; set; }
    public string? LastUpdateAgent { get; set; }
}