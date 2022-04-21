using UnityEngine;

public class PhysicsEnvSettings : MonoBehaviour
{
    public const string EpeeTipTag = "Epee Tip";
    public const string TargetAreaTag = "Target Area";
    public const string FencerOneBodyLayer = "Player 1 Body Layer";
    public const string FencerOneWeaponLayer = "Player 1 Weapon Layer";
    public const string FencerTwoBodyLayer = "Player 2 Body Layer";
    public const string FencerTwoWeaponLayer = "Player 2 Weapon Layer";
    public const int ScaleFactor = 100;
    public const string EnvironmentLayer = "Environment";
    
    public static string GetFencerBodyLayer(int fencerNum)
    {
        return fencerNum == 1 ? FencerOneBodyLayer : FencerTwoBodyLayer;
    }
    
    public static string GetFencerWeaponLayer(int fencerNum)
    {
        return fencerNum == 1 ? FencerOneWeaponLayer : FencerTwoWeaponLayer;
    }

    public static int GetOther(int fencerNum)
    {
        return fencerNum == 1 ? 2 : 1;
    }

}
