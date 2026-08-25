using System.Reflection;

namespace Ravenfield.AiTick.Tests;

public sealed class LethalityPolicyTests
{
    private const int AutoRifle = 0;
    private const int Sniper = 1;
    private const int Handgun = 2;
    private const int Shotgun = 3;
    private const int AutoCannon = 4;
    private const int RocketLauncher = 5;
    private const int GrenadeLauncher = 6;
    private const int MissileLauncher = 7;
    private const int AntiAir = 8;
    private const int DogfightGuns = 9;
    private const int Utility = 10;
    private const int Grenade = 11;
    private const int Mortar = 12;
    private const int Melee = 13;
    private const int SemiAutoRifle = 14;

    [Theory]
    [InlineData(AutoRifle)]
    [InlineData(SemiAutoRifle)]
    [InlineData(Sniper)]
    [InlineData(Shotgun)]
    public void DirectInfantryHitsFromOrdinaryLongGunsAreLethal(int weaponRole)
    {
        Assert.True(ShouldForceLethal(weaponRole: weaponRole));
    }

    [Fact]
    public void DirectBodyHitFromRifleOrSmgIsLethal()
    {
        Assert.True(ShouldForceLethal(weaponRole: AutoRifle));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(39.999f)]
    public void HandgunHitBelowConfiguredFarDistanceIsLethal(float distanceMeters)
    {
        Assert.True(ShouldForceLethal(
            weaponRole: Handgun,
            distanceMeters: distanceMeters,
            handgunNonFatalBeyondMeters: 40f));
    }

    [Theory]
    [InlineData(40f)]
    [InlineData(100f)]
    public void HandgunHitAtOrBeyondConfiguredFarDistanceIsNotForcedLethal(float distanceMeters)
    {
        Assert.False(ShouldForceLethal(
            weaponRole: Handgun,
            distanceMeters: distanceMeters,
            handgunNonFatalBeyondMeters: 40f));
    }

    [Fact]
    public void DisabledPolicyDoesNotForceLethality()
    {
        Assert.False(ShouldForceLethal(enabled: false));
    }

    [Fact]
    public void NonProjectileDamageDoesNotForceLethality()
    {
        Assert.False(ShouldForceLethal(directProjectile: false));
    }

    [Fact]
    public void SplashDamageDoesNotForceLethality()
    {
        Assert.False(ShouldForceLethal(isSplash: true));
    }

    [Fact]
    public void NonInfantryTargetsDoNotUseInfantryLethality()
    {
        Assert.False(ShouldForceLethal(targetIsInfantry: false));
    }

    [Fact]
    public void HeroArmorIsNotBypassed()
    {
        Assert.False(ShouldForceLethal(hasHeroArmor: true));
    }

    [Fact]
    public void PlayerIsExcludedWhenAffectPlayerIsDisabled()
    {
        Assert.False(ShouldForceLethal(targetIsAi: false, affectPlayer: false));
    }

    [Fact]
    public void PlayerIsIncludedWhenAffectPlayerIsEnabled()
    {
        Assert.True(ShouldForceLethal(targetIsAi: false, affectPlayer: true));
    }

    [Theory]
    [InlineData(AutoCannon)]
    [InlineData(RocketLauncher)]
    [InlineData(GrenadeLauncher)]
    [InlineData(MissileLauncher)]
    [InlineData(AntiAir)]
    [InlineData(DogfightGuns)]
    [InlineData(Utility)]
    [InlineData(Grenade)]
    [InlineData(Mortar)]
    [InlineData(Melee)]
    public void NonOrdinaryOrMountedWeaponRolesAreExcluded(int weaponRole)
    {
        Assert.False(ShouldForceLethal(weaponRole: weaponRole));
    }

    [Fact]
    public void MisclassifiedPrimaryLongGunRemainsLethalAtLongRange()
    {
        Assert.True(ShouldForceLethalWithWeaponContext(
            weaponRole: Handgun,
            distanceMeters: 100f,
            handgunNonFatalBeyondMeters: 40f,
            isTrueHandgun: false));
    }

    [Fact]
    public void TrueHandgunAtConfiguredFarDistanceIsNotForcedLethal()
    {
        Assert.False(ShouldForceLethalWithWeaponContext(
            weaponRole: Handgun,
            distanceMeters: 40f,
            handgunNonFatalBeyondMeters: 40f,
            isTrueHandgun: true));
    }

    [Fact]
    public void MountedAutoRifleIsExcluded()
    {
        Assert.False(ShouldForceLethalWithWeaponContext(
            weaponRole: AutoRifle,
            isMountedWeapon: true));
    }

    private static bool ShouldForceLethal(
        bool enabled = true,
        bool directProjectile = true,
        bool isSplash = false,
        bool targetIsInfantry = true,
        bool targetIsAi = true,
        bool affectPlayer = false,
        bool hasHeroArmor = false,
        int weaponRole = AutoRifle,
        float distanceMeters = 10f,
        float handgunNonFatalBeyondMeters = 40f)
    {
        var policyType = typeof(LethalityPolicyTests).Assembly.GetType("Ravenfield.AiTick.LethalityPolicy");
        Assert.NotNull(policyType);

        var method = policyType.GetMethod(
            "ShouldForceLethal",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(int),
                typeof(float),
                typeof(float),
            ],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(
            obj: null,
            parameters:
            [
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
            ]);

        return Assert.IsType<bool>(result);
    }

    private static bool ShouldForceLethalWithWeaponContext(
        bool enabled = true,
        bool directProjectile = true,
        bool isSplash = false,
        bool targetIsInfantry = true,
        bool targetIsAi = true,
        bool affectPlayer = false,
        bool hasHeroArmor = false,
        int weaponRole = AutoRifle,
        float distanceMeters = 10f,
        float handgunNonFatalBeyondMeters = 40f,
        bool isTrueHandgun = false,
        bool isMountedWeapon = false)
    {
        var policyType = typeof(LethalityPolicyTests).Assembly.GetType("Ravenfield.AiTick.LethalityPolicy");
        Assert.NotNull(policyType);

        var method = policyType.GetMethod(
            "ShouldForceLethal",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types:
            [
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(int),
                typeof(float),
                typeof(float),
                typeof(bool),
                typeof(bool),
            ],
            modifiers: null);
        Assert.NotNull(method);

        var result = method.Invoke(
            obj: null,
            parameters:
            [
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
                isTrueHandgun,
                isMountedWeapon,
            ]);

        return Assert.IsType<bool>(result);
    }
}
