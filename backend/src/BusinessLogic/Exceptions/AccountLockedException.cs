namespace TaskManagementSystem.BusinessLogic.Exceptions;

public class AccountLockedException : Exception
{
    public DateTime LockedUntil { get; }

    public AccountLockedException(DateTime lockedUntil)
        : base($"Account is locked until {lockedUntil:O} (UTC).")
    {
        LockedUntil = lockedUntil;
    }
}
