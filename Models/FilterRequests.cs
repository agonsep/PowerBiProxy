namespace PowerBiProxy.Models;

public record AccountIdsFilterRequest(IList<int> AccountIds);

public record CityDateFilterRequest(
    string?   DepartCity,
    string?   ArriveCity,
    DateOnly? IssuedDateFrom,
    DateOnly? IssuedDateTo
);

// Combined request for the /ask endpoint — question + any subset of filters.
public record AskRequest(
    string       Question,
    IList<int>?  AccountIds,
    string?      DepartCity,
    string?      ArriveCity,
    DateOnly?    IssuedDateFrom,
    DateOnly?    IssuedDateTo
);
