namespace DENSOKB.Models
{
    public class MenuTypeDto:MenuDto
    {
        public MenuDto menu { get; set; }
        public string  type { get; set; }
        
    }
}