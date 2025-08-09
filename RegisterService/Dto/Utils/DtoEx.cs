using Newtonsoft.Json;
using WorkerService1.Dto.Interface;

namespace WorkerService1.Dto.Utils;

public static class DtoEx
{
    public static string ToJsonString(this IDto dto)
    {
        return JsonConvert.SerializeObject(dto);
    }

    public static IDto? GetDtoObject(this string rawValue)
    {
        return JsonConvert.DeserializeObject<IDto>(rawValue);
    }
}