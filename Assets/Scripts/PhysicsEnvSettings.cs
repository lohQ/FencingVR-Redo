using UnityEngine;

public class PhysicsEnvSettings : MonoBehaviour
{
    public const string EpeeTipTag = "Epee Tip";
    public const string TargetAreaTag = "Target Area";
    public const string GreenFencerBodyLayer = "Player 1 Body Layer";
    public const string GreenFencerWeaponLayer = "Player 1 Weapon Layer";
    public const string RedFencerBodyLayer = "Player 2 Body Layer";
    public const string RedFencerWeaponLayer = "Player 2 Weapon Layer";
    public const int ScaleFactor = 100;
    public const string EnvironmentLayer = "Environment";

    public static string GetFencerBodyLayer(FencerColor fencerColor)
    {
        return fencerColor == FencerColor.Green ? GreenFencerBodyLayer : RedFencerBodyLayer;
    }
    
    public static string GetFencerWeaponLayer(FencerColor fencerColor)
    {
        return fencerColor == FencerColor.Green ? GreenFencerWeaponLayer : RedFencerWeaponLayer;
    }

    public static FencerColor GetOther(FencerColor fencerColor)
    {
        return fencerColor == FencerColor.Green ? FencerColor.Red : FencerColor.Green;
    }

}
