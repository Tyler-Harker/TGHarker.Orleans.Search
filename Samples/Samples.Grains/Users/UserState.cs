using Samples.Abstractions.Users;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace Samples.Grains;

[Searchable(typeof(IUserGrain))]
[GenerateSerializer]
public class UserState
{
    [Queryable]
    [Id(0)]
    public string Email { get; set; } = string.Empty;

    [Queryable]
    [FullTextSearchable(Weight = 2.0)]
    [Id(1)]
    public string DisplayName { get; set; } = string.Empty;

    [Queryable]
    [Id(2)]
    public bool IsActive { get; set; }
}
