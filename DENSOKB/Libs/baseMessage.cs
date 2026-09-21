using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DENSOKB.Libs
{
    public class baseMessage
    {
        [DataMember(Name = "success", EmitDefaultValue = true, IsRequired = true, Order = 0)]
        public bool success { get; set; } = true;
        [DataMember(Name = "status", EmitDefaultValue = true, IsRequired = true, Order = 1)]
        public int status { get; set; } = 200;

        [DataMember(Name = "result", EmitDefaultValue = true, IsRequired = true, Order = 2)]
        public string result { get; set; } = "success";

        [DataMember(Name = "message", EmitDefaultValue = true, IsRequired = true, Order = 3)]
        public string message { get; set; } = string.Empty;

        //[DataMember(Name = "data", EmitDefaultValue = true, IsRequired = true, Order = 4)]
        //public object data = new object();

        //[DataMember(Name = "fileds", EmitDefaultValue = true, IsRequired = true, Order = 5)]
        //public object fileds = new object();

        //[DataMember(Name = "cols", EmitDefaultValue = true, IsRequired = true, Order = 6)]
        //public object cols = new object();

    }
}