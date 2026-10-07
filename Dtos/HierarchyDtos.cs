namespace SolarGenerationApi.Dtos;

public record ProvinceDto(int Id, string Name);
public record DistrictDto(int Id, string Name, int ProvinceId);
public record GridSubstationDto(int Id, string Name, string Code, int DistrictId);
public record ListResponse<T>(int Total, IReadOnlyList<T> Items);