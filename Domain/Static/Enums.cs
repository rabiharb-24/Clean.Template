namespace Domain.Static;

public enum MaritalStatus
{
    Single = 1,
    Married,
    Divorced,
    Widowed,
    Separated,
    Annulled,
}

public enum Gender
{
    Male = 1,
    Female,
}

public enum Roles
{
    Admin = 1,
    Candidate
}

public enum OrderBy
{
    Asc = 1,
    Desc,
}

public enum TwoFactorTypes
{
    Email = 1,
    AuthenticatorApp,
}