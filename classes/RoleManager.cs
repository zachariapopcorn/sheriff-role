using UnityEngine;
using ButtonManager;

namespace RoleManager
{
    class SheriffRole
    {
        public static string name = "Sheriff";
        public static Color roleColor = new Color(1, (float)(204.0 / 255.0), 0, 1);
        public static byte sheriffID;
        public static int SetSheriff = 40;
        public static CooldownButton sheriifKillButton;
    }
}