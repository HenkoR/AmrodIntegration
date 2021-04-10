using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Utils
{
    public class ProgressTracker
    {
        private static readonly Hashtable Status = new Hashtable();

        public static object GetValue(string itemId)
        {
            return Status[itemId];
        }

        public static void Add(string ItemId, object oStatus)
        {
            //Make sure that oStatus contains only the values 0 through 100 or -1
            Status[ItemId] = oStatus;
            Console.WriteLine(oStatus);
        }

        public static void Update(string ItemId, object oStatus)
        {
            //Make sure that oStatus contains only the values 0 through 100 or -1
            Status[ItemId] = oStatus;
        }

        public static void Remove(string ItemId)
        {
            Status.Remove(ItemId);
        }

        public static bool Contains(string ItemId)
        {
            return Status.Contains(ItemId);
        }

    }
}
