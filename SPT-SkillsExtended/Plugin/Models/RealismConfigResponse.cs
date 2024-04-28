namespace SkillsExtended.Models
{
    /// <summary>
    /// Struct containing realisms settings for compatibility checks
    /// </summary>
    public struct RealismConfig
    {
        public bool recoil_attachment_overhaul { get; set; }
        public bool malf_changes { get; set; }
        public bool realistic_ballistics { get; set; }
        public bool med_changes { get; set; }
        public bool headset_changes { get; set; }
        public bool enable_stances { get; set; }
        public bool movement_changes { get; set; }
        public bool gear_weight { get; set; }
        public bool reload_changes { get; set; }
        public bool manual_chambering { get; set; }
        public bool food_changes { get; set; }
    }
}