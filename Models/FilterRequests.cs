namespace PowerBiProxy.Models;

public record AccountIdsFilterRequest(IList<string> AccountIds);

public record CityDateFilterRequest(
    string?   DepartCity,
    string?   ArriveCity,
    DateOnly? IssuedDateFrom,
    DateOnly? IssuedDateTo
);
