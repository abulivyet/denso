using System.Collections.Generic;

namespace DENSOKB.Models
{
    public class ConfirmBundle
    {
        public string       orderCode { get; set; }

        public string InDate { get; set; }

        public List<Bundle> bundles    { get; set; }
        
        public ConfirmBundle()
        {
            bundles = new List<Bundle>(); 
        }
    }
}