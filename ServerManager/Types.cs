using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerManager
{
    public static class SharedStructs
    {
        //[Serializable]
        public class ComputerInfo
        {
            public string cpu { get; set; }
            public string gpu { get; set; }
            public string mboard { get; set; }
        }
    }

    public class ProductInfo
    {
        public enum Products
        {
            REMOVED = 1228,
            REMOVED1 = 1337,
            REMOVED2 = 1943,
            REMOVED3 = 1446,
            REMOVED4 = 1955,
            REMOVED5 = 1451,
            REMOVED6 = 2121,
            REMOVED7 = 6970,
            REMOVED8 = 1973,
            MASTER_SERVER = 50210,
            OSU = 26247
        }
    }

    public class clsComputerAccess
    {
        public string accessTime { get; set; }
        public byte[] computerHWID { get; set; }
        public SharedStructs.ComputerInfo computer { get; set; }
        public List<string> accessInfo { get; set; }
    }

    public class clsBuild
    {
        public ProductInfo.Products product { get; set; }
        public DateTime lastBuildDate { get; set; }
        public string currentClientMD5 { get; set; }
        public List<string> oldClientMD5s { get; set; }
        public bool buildRequired { get; set; } // build required on next logon.
    }

    public class clsSteamAccount
    {
        public string steam64 { get; set; }
        public string profileURL { get; set; }
        public string accountName { get; set; }
        public string personalName { get; set; }
        public string rememberPassword { get; set; }
        public string lastLogin { get; set; }
        public string timestamp { get; set; }
    }

    public class clsClient
    {
        public string sUsername { get; set; }
        public byte[] bPassword { get; set; }
        public byte[] bHWID { get; set; }
        public byte[] bSecondaryHWID { get; set; }
        public bool allowTwoHWIDs { get; set; }
        public bool bypassMaintenanceMode { get; set; }
        public bool bypassMD5Check { get; set; }
        public bool betaTester { get; set; }
        public bool hwidLocked { get; set; }
        public bool banned { get; set; }
        public List<string> accessedFrom { get; set; }
        public List<clsComputerAccess> accessedFromComputer { get; set; }
        public List<string> attemptedAccessFrom { get; set; }
        public List<clsComputerAccess> attemptedComputerFrom { get; set; }
        public List<string> skypeAccounts { get; set; }
        public List<byte[]> badHWID { get; set; }
        public string dtLastLogin { get; set; }
        public List<string> keys { get; set; }
        public List<clsBuild> builds { get; set; }
        public List<clsSteamAccount> steamAccounts { get; set; }
    }

    public class UserWarnFile
    {
        public string userid { get; set; }
        public int warnings { get; set; }
    }
}
