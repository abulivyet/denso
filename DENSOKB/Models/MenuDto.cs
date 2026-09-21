using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DENSOKB.Models
{
    public class MenuDto
    {
        public string        id         { get; set; }
        public string        parentid   { get; set; }
        [Column("title")]
        public string        text      { get; set; }       
        public string        url        { get; set; }
        [Column("IsLeaf")]
        public bool          leaf       { get; set; }
        public bool          expanded   { get; set; }
        public string        iconCls    { get; set; }
        public int           orderIndex { get; set; }
        public List<MenuDto> children   { get; set; } = new List<MenuDto>();
    }

}