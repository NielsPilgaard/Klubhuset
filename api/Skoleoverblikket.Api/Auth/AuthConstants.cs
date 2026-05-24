namespace Skoleoverblikket.Api.Auth;

public static class Roles
{
	public const string Admin = "admin";
	public const string Parent = "parent";
	public const string SuperAdmin = "superadmin";
}

public static class Policies
{
	public const string EditClass = "EditClass";
	public const string EditWeekPlan = "EditWeekPlan";
	public const string ParentClassAccess = "ParentClassAccess";
}
