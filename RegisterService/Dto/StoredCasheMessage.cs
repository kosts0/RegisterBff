using WorkerService1.Dto.Interface;

namespace WorkerService1.Dto;

public class StoredCasheMessage<TMessage> : IDto
{
    public TMessage Message { get; set; }
    public Guid MessageId { get; set; }
}