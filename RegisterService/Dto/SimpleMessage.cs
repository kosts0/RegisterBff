using WorkerService1.Dto.Interface;

namespace WorkerService1.Dto;

public class SimpleMessage : IDto
{
    public string MessageValue { get; set; }
}