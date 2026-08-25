namespace Ravenfield.AiTick;

/// <summary>
/// Selects ordinary direct firearm hits that use infantry one-hit lethality.
/// Weapon role values match Ravenfield's WeaponRole enum.
/// </summary>
public static class LethalityPolicy
{
    public const float DefaultHandgunNonFatalBeyondMeters = 40f;

    private const int AutoRifle = 0;
    private const int Sniper = 1;
    private const int Handgun = 2;
    private const int Shotgun = 3;
    private const int SemiAutoRifle = 14;

    public static bool ShouldForceLethal(
        bool enabled,
        bool directProjectile,
        bool isSplash,
        bool targetIsInfantry,
        bool targetIsAi,
        bool affectPlayer,
        bool hasHeroArmor,
        int weaponRole,
        float distanceMeters,
        float handgunNonFatalBeyondMeters)
    {
        return ShouldForceLethal(
            enabled,
            directProjectile,
            isSplash,
            targetIsInfantry,
            targetIsAi,
            affectPlayer,
            hasHeroArmor,
            weaponRole,
            distanceMeters,
            handgunNonFatalBeyondMeters,
            isTrueHandgun: weaponRole == Handgun,
            isMountedWeapon: false);
    }

    public static bool ShouldForceLethal(
        bool enabled,
        bool directProjectile,
        bool isSplash,
        bool targetIsInfantry,
        bool targetIsAi,
        bool affectPlayer,
        bool hasHeroArmor,
        int weaponRole,
        float distanceMeters,
        float handgunNonFatalBeyondMeters,
        bool isTrueHandgun,
        bool isMountedWeapon)
    {
        if (!enabled
            || !directProjectile
            || isSplash
            || !targetIsInfantry
            || (!targetIsAi && !affectPlayer)
            || hasHeroArmor
            || isMountedWeapon)
        {
            return false;
        }

        if (weaponRole == Handgun && isTrueHandgun)
        {
            return distanceMeters < handgunNonFatalBeyondMeters;
        }

        return weaponRole == AutoRifle
            || weaponRole == SemiAutoRifle
            || weaponRole == Sniper
            || weaponRole == Shotgun
            || weaponRole == Handgun;
    }
}
